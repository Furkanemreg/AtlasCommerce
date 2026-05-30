using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using AtlasCommerce.Persistance.Migrations;
using AtlasCommerce.Persistance.Services;
using AtlasCommerce.UI.Controllers.Base;
using AtlasCommerce.UI.Controllers.Products;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using NuGet.Configuration;
using System;

namespace AtlasCommerce.UI.Controllers
{
    public class OrderController : BaseController<Order>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<OrderController> _logger;
        private readonly IMapper _mapper;
        private readonly IImageService _imageService;
        private readonly IEmailService _emailService;
        private readonly IBaseService<Product> _productService;
        private readonly IBaseService<Payment> _paymentService;
        private readonly IBaseService<Category> _categoryService;
        private readonly IBaseService<WebsiteSettings> _settingsService;
        private readonly ICurrentUserService _currentUserService; 
        private readonly IMemoryCache _memoryCache;

        public OrderController(
            UserManager<AppUser> userManager,
            IBaseService<Order> baseService, 
            IUnitOfWork unitOfWork, 
            IBaseService<Product> productService,
            IBaseService<Payment> paymentService,
            IBaseService<Category> categoryService, 
            IBaseService<WebsiteSettings> settingsService,
            ICurrentUserService currentUserService,
            ILogger<OrderController> logger, 
            IMapper mapper, 
            IImageService imageService, 
            IEmailService emailService,
            IMemoryCache memoryCache)
            : base(baseService, unitOfWork)
        {
            _userManager = userManager;
            _logger = logger;
            _mapper = mapper;
            _imageService = imageService;
            _emailService = emailService;
            _productService = productService;
            _paymentService = paymentService;
            _categoryService = categoryService;
            _settingsService = settingsService;
            _currentUserService = currentUserService;
            _memoryCache = memoryCache;
        }

        public static string DetectCardBrand(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                return "Unknown";

            var number = new string(cardNumber.Where(char.IsDigit).ToArray());

            if (number.StartsWith("4"))
                return "VISA";

            if (number.Length >= 2)
            {
                var firstTwo = int.Parse(number.Substring(0, 2));

                if (firstTwo >= 51 && firstTwo <= 55)
                    return "MASTERCARD";
            }

            if (number.Length >= 4)
            {
                var firstFour = int.Parse(number.Substring(0, 4));

                if (firstFour >= 2221 && firstFour <= 2720)
                    return "MASTERCARD";
            }

            if (number.StartsWith("34") || number.StartsWith("37"))
                return "AMEX";

            if (number.StartsWith("6011") || number.StartsWith("65"))
                return "DISCOVER";

            // TROY (genelde 9792 ile başlar)
            if (number.StartsWith("9792"))
                return "TROY";

            return "Unknown";
        }

        public static string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 4)
                return "Unknown";

            var last4 = cardNumber[^4..];

            return $"**** **** **** {last4}";
        }

        private async Task LoadProductsToDropdown()
        {
            // Navbardaki her kategori için ürünlerini getir
            var dropdownCategories = await _categoryService.GetAllAsync(x => x.ShowInDropDown == true && x.IsActive == true);
            var categoryWithProducts = new List<CategoryWithProductsVM>();

            foreach (var cat in dropdownCategories)
            {
                var products = await _productService.GetAllAsync(p => p.CategoryId == cat.Id && p.IsActive);
                var productVMs = new List<SaleProductVM>();

                foreach (var p in products)
                {
                    string? imageDataUri = null;
                    var img = await _imageService.GetByOwnerAsync(p.Id, nameof(Product));
                    if (img?.Data != null)
                        imageDataUri = $"data:image/png;base64,{Convert.ToBase64String(img.Data)}";

                    productVMs.Add(new SaleProductVM
                    {
                        Id = p.Id,
                        Title = p.Name!,
                        Barcode = p.Barcode!,
                        SalePrice = p.SalePriceIncludingTaxes,
                        CategoryId = p.CategoryId
                    });
                }

                categoryWithProducts.Add(new CategoryWithProductsVM
                {
                    Id = cat.Id,
                    Name = cat.Name!,
                    Products = productVMs
                });
            }

            ViewBag.CategoryWithProducts = categoryWithProducts;
        }

        private void SetCart()
        {
            var sessionCart = HttpContext.Session.GetString("CartSession");
            ViewBag.CartItemCount = !string.IsNullOrEmpty(sessionCart)
                ? JsonConvert.DeserializeObject<List<CartItemVM>>(sessionCart)!.Sum(x => x.Quantity)
                : 0;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = _currentUserService.UserId();

            await LoadProductsToDropdown();
            SetCart();

            var orders = (await _baseService.GetAllAsync(x => x.Items))?.Where(i => i.UserId == currentUserId);

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            var settingsVm = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            var vm = new OrderListVM
            {
                Orders = orders?.Select(x => new OrderSummaryVM
                {
                    Id = x.Id,
                    OrderNumber = x.OrderNumber,
                    TotalAmount = x.TotalAmount,
                    ItemCount = x.Items?.Count ?? 0,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt,
                    IsPickup = x.IsPickup
                }).ToList(),

                Settings = settingsVm
            };

            return View(vm);
        }

        public async Task<IActionResult> Success()
        {
            await LoadProductsToDropdown();
            SetCart();

            return View();
        }

        // GET CART FROM THE SESSION
        private List<CartItemVM> GetCart()
        {
            var sessionCart = HttpContext.Session.GetString("CartSession");

            if (string.IsNullOrEmpty(sessionCart))
                return new List<CartItemVM>();

            return JsonConvert.DeserializeObject<List<CartItemVM>>(sessionCart)!;
        }

        // Save Order (DRAFT)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SaveDraft()
        {
            var currentUserId = _currentUserService.UserId();
            var cart = GetCart();

            if (!cart.Any())
                return Json(new { success = false, message = "Sepetinizde ürün bulunmuyor." });

            var productIds = cart.Select(x => x.ProductId).ToList();
            var products = await _productService.GetAllAsync(p => productIds.Contains(p.Id));

            var defaultAddress = "Varsayılan Satıcı Adresi";
            var defaultCountry = "Varsayılan Ülke";
            var defaultCity = "Varsayılan İl";
            var defaultDistrict = "Varsayılan İlçe";
            var defaultZipCode = "Varsayılan Posta Kodu";

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            if (settingsEntity != null)
            {
                defaultAddress = settingsEntity.Address ?? "Varsayılan Satıcı Adresi";
                defaultCountry = settingsEntity.Country ?? "Varsayılan Ülke";
                defaultCity = settingsEntity.City ?? "Varsayılan İl";
                defaultDistrict = settingsEntity.District ?? "Varsayılan İlçe";
                defaultZipCode = settingsEntity.ZipCode ?? "Varsayılan Posta Kodu";
            }

            var order = new Order
            {
                UserId = currentUserId,
                Status = enmOrderStatus.Draft,
                CreatedAt = DateTime.Now,
                OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}",

                // KARGO ENTEGRASYONU OLMADIĞI İÇİN ŞUAN MAĞAZADA TESLİM
                ShippingAddress = defaultAddress,
                Country = defaultCountry,
                City = defaultCity,
                District = defaultDistrict,
                ZipCode = defaultZipCode,
                IsPickup = true,

                Items = new List<OrderItem>()
            };

            try
            {
                foreach (var item in cart)
                {
                    var product = products.FirstOrDefault(p => p.Id == item.ProductId);

                    if (product == null)
                        return Json(new { success = false, message = "Ürün bulunamadı." });

                    if (product.StockQuantity < item.Quantity)
                        return Json(new { success = false, message = $"Stok yetersiz: {product.Name}" });

                    // PRICE SNAPSHOT
                    order.Items.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Barcode = product.Barcode,

                        UnitPriceExclTax = product.SalePriceExcludingTaxes,
                        UnitPriceInclTax = product.SalePriceIncludingTaxes,

                        TaxRate = product.TaxRate,
                        OTVRate = product.OTV,

                        Quantity = item.Quantity,
                        ImageUrl = item.ImageUrl
                    });
                }

                // TOTALS
                order.SubTotal = order.Items.Sum(x => x.SubTotal);

                var totalTax = order.Items.Sum(x => x.TaxAmount);
                var totalOtv = order.Items.Sum(x => x.OTVAmount);

                order.TotalAmount = order.SubTotal + totalTax + totalOtv;

                await _baseService.AddAsync(order);
                await _unitOfWork.Commit();

                _logger.LogInformation("Draft order created: {OrderId}", order.Id);

                HttpContext.Session.Remove("CartSession");

                return RedirectToAction("Detail", new { id = order.Id });
            }
            catch (Exception ex)
            {
                await _unitOfWork.Rollback();

                _logger.LogError(ex, "Draft order creation failed.");

                return Json(new { success = false, message = "Taslak sipariş oluşturulamadı." });
            }
        }

        // Checkout => ORDER (Pending => Paid akışı başlangıcı)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrderForPayment(
            bool isPickup,
            string shippingAddress,
            string country,
            string city,
            string district,
            string? zipCode,
            string requestId,
            string cardNumber,
            string nameOnCard,
            string exp,
            string cvv)
        {
            var currentUserId = _currentUserService.UserId();

            if (isPickup == false)
            {
                return BadRequest("Şuan sadece mağazada teslim seçeneği mevcuttur.");
            }

            var defaultAddress = "Varsayılan Satıcı Adresi";
            var defaultCountry = "Varsayılan Ülke";
            var defaultCity = "Varsayılan İl";
            var defaultDistrict = "Varsayılan İlçe";
            var defaultZipCode = "Varsayılan Posta Kodu";

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            if (settingsEntity != null)
            {
                defaultAddress = settingsEntity.Address ?? "Varsayılan Satıcı Adresi";
                defaultCountry = settingsEntity.Country ?? "Varsayılan Ülke";
                defaultCity = settingsEntity.City ?? "Varsayılan İl";
                defaultDistrict = settingsEntity.District ?? "Varsayılan İlçe";
                defaultZipCode = settingsEntity.ZipCode ?? "Varsayılan Posta Kodu";
            }

            if (string.IsNullOrWhiteSpace(requestId))
                return BadRequest("Request ID alınamadı.");
            if (string.IsNullOrWhiteSpace(cardNumber))
                return BadRequest("Lütfen kart numaranızı giriniz.");
            if (string.IsNullOrWhiteSpace(nameOnCard))
                return BadRequest("Lütfen kart üzerindeki ismi giriniz.");
            if (string.IsNullOrWhiteSpace(exp))
                return BadRequest("Lütfen kartınızın son kullanma tarihini giriniz.");
            if (string.IsNullOrWhiteSpace(cvv))
                return BadRequest("Lütfen kartınızın CVV bilgisini giriniz.");

            if (cardNumber.Length < 16)
                return BadRequest("Kart numarası 16 haneden az olamaz.");
            if (exp.Length < 5)
                return BadRequest("Lütfen geçerli bir son kullanma tarihi giriniz.");
            if (cvv.Length < 3)
                return BadRequest("CVV 3 haneden az olamaz.");

            // 1) Basic validation
            if (string.IsNullOrWhiteSpace(shippingAddress) ||
                string.IsNullOrWhiteSpace(district) ||
                string.IsNullOrWhiteSpace(city) ||
                string.IsNullOrWhiteSpace(country))
            {
                return BadRequest("Lütfen eksik adres bilgilerinizi giriniz.");
            }

            // 2) Double submit protection
            var cacheKey = $"order_req_{requestId}";
            if (_memoryCache.TryGetValue(cacheKey, out _))
            {
                return BadRequest("Bu işlem zaten gerçekleştirildi.");
            }
            _memoryCache.Set(cacheKey, true, TimeSpan.FromMinutes(5));

            // 3) GET CART
            var cart = GetCart();
            if (!cart.Any())
                return RedirectToAction("Index", "Cart");

            // 4) GET PRODUCTS AT ONCE (N+1)
            var productIds = cart.Select(x => x.ProductId).ToList();
            var products = await _productService.GetAllAsync(p => productIds.Contains(p.Id));

            // 5) Order oluştur
            var order = new Order
            {
                UserId = currentUserId,
                Status = enmOrderStatus.PendingPayment,
                CreatedAt = DateTime.Now,

                // KARGO ENTEGRASYONU OLMADIĞI İÇİN ŞUAN MAĞAZADA TESLİM
                ShippingAddress = defaultAddress,
                Country = defaultCountry,
                City = defaultCity,
                District = defaultDistrict,
                ZipCode = defaultZipCode,
                IsPickup = true,

                OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}",

                Items = new List<OrderItem>()
            };
            
            try
            {
                foreach (var item in cart)
                {
                    var product = products.FirstOrDefault(p => p.Id == item.ProductId);

                    if (product == null)
                        return BadRequest("Ürün bulunamadı.");

                    // 6) STOCK CONTROL
                    if (product.StockQuantity < item.Quantity)
                        return BadRequest($"Stok yetersiz: {product.Name}");

                    // 7) PRICE CONTROL (manipülasyon koruması)
                    var currentPrice = product.SalePriceIncludingTaxes;
                    if (item.PriceIncludingTaxes != currentPrice)
                        return BadRequest("Fiyat değişti, lütfen sayfayı yenileyin.");

                    order.Items.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Barcode = product.Barcode,

                        UnitPriceExclTax = product.SalePriceExcludingTaxes,
                        UnitPriceInclTax = product.SalePriceIncludingTaxes,
                        TaxRate = product.TaxRate,
                        OTVRate = product.OTV,

                        Quantity = item.Quantity
                    });
                }

                // 8) Totals
                order.SubTotal = order.Items.Sum(x => x.SubTotal);

                var totalTax = order.Items.Sum(x => x.TaxAmount);
                var totalOtv = order.Items.Sum(x => x.OTVAmount);

                order.TotalAmount = order.SubTotal + totalTax + totalOtv;

                // 9) Save
                await _baseService.AddAsync(order);
                await _unitOfWork.Commit();

                _logger.LogInformation("Order created: {OrderId}", order.Id);

                // PAYMENT CREATE
                var payment = new Payment
                {
                    OrderId = order.Id,
                    Amount = order.TotalAmount,
                    Status = enmPaymentStatus.Pending,
                    CreatedAt = DateTime.Now,
                    TransactionId = Guid.Parse(requestId),
                    CardBrand = !string.IsNullOrWhiteSpace(cardNumber) ? DetectCardBrand(cardNumber) : "Unknown",
                    CardNumber = MaskCardNumber(cardNumber) ?? "Unknown"
                };

                await _paymentService.AddAsync(payment);
                await _unitOfWork.Commit();

                _logger.LogInformation("Payment created: {PaymentId}", payment.Id);

                return Json(new { orderId = order.Id, paymentId = payment.Id });
            }
            catch (Exception ex)
            {
                await _unitOfWork.Rollback();

                _logger.LogError(ex, "Order creation failed.");

                return StatusCode(500, "Sipariş oluşturulamadı.");
            }
        }

        // PAYMENT successfull => Update ORDER
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletePayment(Guid paymentId)
        {
            try 
            {
                var payment = await _paymentService.GetByIdAsync(paymentId);

                if (payment == null)
                    return NotFound("Ödeme bulunamadı.");

                if (payment.Status != enmPaymentStatus.Pending)
                {
                    payment.FailedAt = DateTime.Now;
                    payment.FailureReason = "Ödeme bekleme aşamasında değil. Tamamlanmış veya iptal edilmiş.";

                    await _paymentService.UpdateAsync(payment);
                    await _unitOfWork.Commit();

                    return BadRequest("Ödeme bekleme aşamasında değil. Tamamlanmış veya iptal edilmiş.");
                }

                payment.Status = enmPaymentStatus.Paid;
                payment.PaidAt = DateTime.Now;

                await _paymentService.UpdateAsync(payment);

                // Order Update
                var order = await _baseService.GetByIdAsync(payment.OrderId);
                if (order == null)
                    return NotFound("Sipariş bulunamadı.");

                order.Status = enmOrderStatus.Paid;

                await _baseService.UpdateAsync(order);
                await _unitOfWork.Commit();

                HttpContext.Session.Remove("CartSession");

                #region ONAY E-POSTASI
                var user = await _userManager.FindByIdAsync(order.UserId.ToString());

                var subject = "Siparişiniz başarıyla alındı";

                var body = $@"
                    <div style='font-family:Arial'>
                        <h2>Teşekkürler {user!.FullName}!</h2>

                        <p>Siparişiniz başarıyla oluşturulmuştur ve ödeme alınmıştır.</p>

                        <h4>Sipariş No: {order.OrderNumber}</h4>
                        <p>Toplam Tutar: ₺ {order.TotalAmount:N2}</p>

                        <p>Siparişiniz hazırlanma sürecine alınmıştır.</p>

                        <br/>

                        <a href='https://localhost:7103/Order/Details/{order.Id}'>
                            Siparişimi Görüntüle
                        </a>

                        <br/><br/>

                        <small>AtlasCommerce</small>
                    </div>
                ";

                await _emailService.SendAsync(user!.Email, subject, body);
                #endregion

                return Ok("Ödemeniz başarıyla işlendi.");
            }
            catch (Exception ex)
            {
                var payment = await _paymentService.GetByIdAsync(paymentId);

                if (payment != null)
                {
                    payment.Status = enmPaymentStatus.Failed;
                    payment.FailedAt = DateTime.Now;
                    payment.FailureReason = $"Sistem Hatası: {ex.Message}";

                    await _paymentService.UpdateAsync(payment);
                    await _unitOfWork.Commit();
                }
                return BadRequest($"Ödeme yapılırken bir hata oluştu: {ex.Message}");
            }
        }

        // Order DETAILS
        public async Task<IActionResult> Detail(Guid id)
        {
            await LoadProductsToDropdown();
            SetCart();

            var order = (await _baseService.GetAllAsync(i => i.Items))
                ?.FirstOrDefault(i => i.Id == id);

            if (order == null)
                return NotFound();

            var payment = (await _paymentService
                .GetAllAsync(x => x.OrderId == order.Id))
                .FirstOrDefault();

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            var settingsVm = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            var vm = new OrderDetailVM
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber ?? "-",
                IsPickup = true, // KARGO ENTEGRASYONU OLMADIĞI İÇİN ŞUAN MAĞAZADA TESLİM

                Address = order.ShippingAddress ?? "-",
                Country = order.Country ?? "-",
                City = order.City ?? "-",
                District = order.District ?? "-",
                ZipCode = order.ZipCode ?? "-",

                SubTotal = order.SubTotal,
                TotalAmount = order.TotalAmount,

                Status = order.Status,
                CreatedAt = order.CreatedAt,

                Items = order.Items?.Select(x => new OrderItemVM
                {
                    OrderId = x.OrderId,
                    ProductId = x.ProductId,
                    ProductName = x.ProductName,
                    Barcode = x.Barcode,

                    UnitPrice = x.UnitPriceInclTax,
                    SubTotal = x.SubTotal,
                    TaxAmount = x.TaxAmount,
                    OTVAmount = x.OTVAmount,

                    Quantity = x.Quantity
                }).ToList() ?? new(),
                
                TotalTAX = order!.Items!.Sum(i => i.TaxAmount + i.OTVAmount),

                Payment = payment == null ? null : new PaymentInfoVM
                {
                    Amount = payment.Amount,
                    Status = payment.Status,
                    CreatedAt = payment.CreatedAt,
                    PaidAt = payment.PaidAt,
                    CardBrand = payment.CardBrand,
                    CardNumber = payment.CardNumber
                },

                Settings = settingsVm
            };

            return View(vm);
        }

        // PAYMENT OF THE "DRAFT" ORDER
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartPayment(Guid orderId)
        {
            var currentUserId = _currentUserService.UserId();

            var order = await _baseService.GetByIdAsync(orderId);

            if (order == null || order.UserId != currentUserId)
                return NotFound();

            if (order.Status != enmOrderStatus.Draft && order.Status != enmOrderStatus.PendingPayment)
                return BadRequest("Bu sipariş ödeme için uygun değil.");

            order.Status = enmOrderStatus.PendingPayment;
            await _baseService.UpdateAsync(order);

            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Status = enmPaymentStatus.Pending,
                CreatedAt = DateTime.Now,
                TransactionId = Guid.NewGuid(),
                CardNumber = "Unknown",
                CardBrand = "Unknown"
            };

            await _paymentService.AddAsync(payment);
            await _unitOfWork.Commit();

            // Ödeme Sayfasına Redirect
            return RedirectToAction("FromOrder", "Checkout", new
            {
                orderId = order.Id,
                paymentId = payment.Id
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDraft(Guid orderId)
        {
            var currentUserId = _currentUserService.UserId();

            var order = await _baseService.GetByIdAsync(orderId);

            if (order == null || order.UserId != currentUserId)
                return NotFound();

            // Sadece taslak/ödeme bekleyen sipariş silinebilir
            if (order.Status != enmOrderStatus.Draft && order.Status != enmOrderStatus.PendingPayment)
                return BadRequest("Sadece taslak/ödeme bekleyen siparişler silinebilir.");

            try
            {
                await _baseService.DeleteAsync(order);
                await _unitOfWork.Commit();

                _logger.LogInformation("Draft order deleted: {OrderId}", order.Id);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await _unitOfWork.Rollback();

                _logger.LogError(ex, "Draft order delete failed.");

                return BadRequest("Taslak sipariş silinemedi.");
            }
        }

        // ? TASLAK / ÖDEME BEKLEYEN SİPARİŞİ DÜZENLE
        //[HttpGet]
        //[Authorize]
        //public async Task<IActionResult> Edit(Guid id)
        //{
        //    var currentUserId = _currentUserService.UserId();

        //    await LoadProductsToDropdown();
        //    SetCart();

        //    var order = (await _baseService.GetAllAsync(i => i.Items))
        //        .FirstOrDefault(x => x.Id == id && x.UserId == currentUserId);

        //    if (order == null)
        //        return NotFound();

        //    if (order.Status != enmOrderStatus.Draft && order.Status != enmOrderStatus.PendingPayment)
        //        return BadRequest("Bu sipariş düzenlenemez.");

        //    var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
        //    var settingsVm = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

        //    var vm = new OrderDetailVM
        //    {
        //        Id = order.Id,
        //        OrderNumber = order.OrderNumber,
        //        IsPickup = order.IsPickup,

        //        Address = order.ShippingAddress,
        //        Country = order.Country,
        //        City = order.City,
        //        District = order.District,
        //        ZipCode = order.ZipCode,

        //        Items = order.Items.Select(x => new OrderItemVM
        //        {
        //            ProductId = x.ProductId,
        //            ProductName = x.ProductName,
        //            Barcode = x.Barcode,
        //            Quantity = x.Quantity,
        //            UnitPrice = x.UnitPriceInclTax
        //        }).ToList(),

        //        Settings = settingsVm
        //    };

        //    return View(vm);
        //}

        //[HttpPost]
        //[Authorize]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(OrderDetailVM model)
        //{
        //    var currentUserId = _currentUserService.UserId();

        //    var order = (await _baseService.GetAllAsync(i => i.Items))
        //        .FirstOrDefault(x => x.Id == model.Id && x.UserId == currentUserId);

        //    if (order == null)
        //        return NotFound();

        //    if (order.Status != enmOrderStatus.Draft &&
        //        order.Status != enmOrderStatus.PendingPayment)
        //    {
        //        return BadRequest("Bu sipariş güncellenemez.");
        //    }

        //    // Address update
        //    order.ShippingAddress = model.Address;
        //    order.Country = model.Country;
        //    order.City = model.City;
        //    order.District = model.District;
        //    order.ZipCode = model.ZipCode;

        //    // ITEM UPDATE (quantity güncelle)
        //    foreach (var item in model.Items)
        //    {
        //        var dbItem = order.Items.FirstOrDefault(x => x.ProductId == item.ProductId);
        //        if (dbItem != null)
        //        {
        //            dbItem.Quantity = item.Quantity;
        //        }
        //    }

        //    // RE-CALCULATE
        //    order.SubTotal = order.Items.Sum(x => x.SubTotal);

        //    var tax = order.Items.Sum(x => x.TaxAmount);
        //    var otv = order.Items.Sum(x => x.OTVAmount);

        //    order.TotalAmount = order.SubTotal + tax + otv;

        //    await _baseService.UpdateAsync(order);
        //    await _unitOfWork.Commit();

        //    return RedirectToAction("Detail", new { id = order.Id });
        //}
    }
}
