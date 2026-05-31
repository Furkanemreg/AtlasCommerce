using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.Interfaces.Caching;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using AtlasCommerce.Persistance.Migrations;
using AtlasCommerce.Persistance.Services;
using AtlasCommerce.Persistance.Services.Caching;
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
        private readonly ICartCacheService _cartCacheService;
        private readonly IDropdownCacheService _dropdownCacheService;
        private readonly ISettingsCacheService _settingsCacheService;

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
            IMemoryCache memoryCache,
            ICartCacheService cartCacheService,
            IDropdownCacheService dropdownCacheService,
            ISettingsCacheService settingsCacheService)
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
            _cartCacheService = cartCacheService;
            _dropdownCacheService = dropdownCacheService;
            _settingsCacheService = settingsCacheService;
        }

        #region CACHING / Common Areas
        private const string CartCookieName = "cart_id";
        private const string DropdownCacheKey = "ui:dropdown:categories";
        private const string SettingsCacheKey = "ui:settings";
        private string GetCartKey()
        {
            var key = "";

            // 1. USER LOGGED IN
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                key = $"cart:user:{userId}";
                return key;
            }

            // 2. ANONYMOUS USER => COOKIE BASED
            if (!Request.Cookies.TryGetValue(CartCookieName, out var cartId) || string.IsNullOrEmpty(cartId))
            {
                cartId = Guid.NewGuid().ToString();

                Response.Cookies.Append(CartCookieName, cartId, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                });
            }

            key = $"cart:anon:{cartId}";

            return key;
        }
        private async Task SetCart()
        {
            var cart =
                await _cartCacheService.GetAsync(GetCartKey());

            ViewBag.CartItemCount = cart.Sum(x => x.Quantity);
        }
        private async Task LoadProductsToDropdown()
        {
            var cached = await _dropdownCacheService.GetAsync();

            if (cached != null)
            {
                ViewBag.CategoryWithProducts = cached;
                return;
            }

            var dropdownCategories = await _categoryService
                .GetAllAsync(x => x.ShowInDropDown && x.IsActive);

            var categoryWithProducts = new List<CategoryWithProductsVM>();

            foreach (var cat in dropdownCategories)
            {
                var products = await _productService
                    .GetAllAsync(p => p.CategoryId == cat.Id && p.IsActive);

                categoryWithProducts.Add(new CategoryWithProductsVM
                {
                    Id = cat.Id,
                    Name = cat.Name!,
                    Products = products.Select(p => new SaleProductVM
                    {
                        Id = p.Id,
                        Title = p.Name!,
                        Barcode = p.Barcode!,
                        SalePrice = p.SalePriceIncludingTaxes,
                        CategoryId = p.CategoryId
                    }).ToList()
                });
            }

            await _dropdownCacheService.SetAsync(
                categoryWithProducts,
                TimeSpan.FromHours(2)
            );

            ViewBag.CategoryWithProducts = categoryWithProducts;
        }
        #endregion

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
        
        public async Task<IActionResult> Index()
        {
            var currentUserId = _currentUserService.UserId();

            await LoadProductsToDropdown();
            await SetCart();

            var orders = (await _baseService.GetAllAsync(x => x.Items))?.Where(i => i.UserId == currentUserId);

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
                }).ToList()
            };

            var settings = await _settingsCacheService.GetAsync();

            if (settings == null)
            {
                var entity = (await _settingsService.GetAllAsync()).FirstOrDefault();
                settings = _mapper.Map<WebsiteSettingsVM>(entity);

                await _settingsCacheService.SetAsync(settings, TimeSpan.FromDays(1));
            }

            vm.Settings = settings;

            return View(vm);
        }

        public async Task<IActionResult> Success()
        {
            await LoadProductsToDropdown();
            await SetCart();

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
                return Json(new { success = false, message = "There are no products in your cart." });

            var productIds = cart.Select(x => x.ProductId).ToList();
            var products = await _productService.GetAllAsync(p => productIds.Contains(p.Id));

            var defaultAddress = "Default Seller Address";
            var defaultCountry = "Default Country";
            var defaultCity = "Default City";
            var defaultDistrict = "Default District";
            var defaultZipCode = "Default Postal Code";

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            if (settingsEntity != null)
            {
                defaultAddress = settingsEntity.Address ?? "Default Seller Address";
                defaultCountry = settingsEntity.Country ?? "Default Country";
                defaultCity = settingsEntity.City ?? "Default City";
                defaultDistrict = settingsEntity.District ?? "Default District";
                defaultZipCode = settingsEntity.ZipCode ?? "Default Postal Code";
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
                        return Json(new { success = false, message = "Product not found." });

                    if (product.StockQuantity < item.Quantity)
                        return Json(new { success = false, message = $"Insufficient stock: {product.Name}" });

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

                return Json(new { success = false, message = "Draft order could not be created." });
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
                return BadRequest("Currently, only in-store pickup option is available.");
            }

            var defaultAddress = "Default Seller Address";
            var defaultCountry = "Default Country";
            var defaultCity = "Default City";
            var defaultDistrict = "Default District";
            var defaultZipCode = "Default Postal Code";

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            if (settingsEntity != null)
            {
                defaultAddress = settingsEntity.Address ?? "Default Seller Address";
                defaultCountry = settingsEntity.Country ?? "Default Country";
                defaultCity = settingsEntity.City ?? "Default City";
                defaultDistrict = settingsEntity.District ?? "Default District";
                defaultZipCode = settingsEntity.ZipCode ?? "Default Postal Code";
            }
            if (string.IsNullOrWhiteSpace(requestId))
                return BadRequest("Request ID could not be retrieved.");

            if (string.IsNullOrWhiteSpace(cardNumber))
                return BadRequest("Please enter your card number.");

            if (string.IsNullOrWhiteSpace(nameOnCard))
                return BadRequest("Please enter the name on your card.");

            if (string.IsNullOrWhiteSpace(exp))
                return BadRequest("Please enter your card's expiration date.");

            if (string.IsNullOrWhiteSpace(cvv))
                return BadRequest("Please enter your card's CVV.");

            if (cardNumber.Length < 16)
                return BadRequest("Card number cannot be less than 16 digits.");

            if (exp.Length < 5)
                return BadRequest("Please enter a valid expiration date.");

            if (cvv.Length < 3)
                return BadRequest("CVV cannot be less than 3 digits.");

            // 1) Basic validation
            if (string.IsNullOrWhiteSpace(shippingAddress) ||
                string.IsNullOrWhiteSpace(district) ||
                string.IsNullOrWhiteSpace(city) ||
                string.IsNullOrWhiteSpace(country))
            {
                return BadRequest("Please fill in your missing address information.");
            }

            // 2) Double submit protection
            var cacheKey = $"order_req_{requestId}";
            if (_memoryCache.TryGetValue(cacheKey, out _))
            {
                return BadRequest("This operation has already been completed.");
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
                        return BadRequest("Product not found.");

                    // 6) STOCK CONTROL
                    if (product.StockQuantity < item.Quantity)
                        return BadRequest($"Insufficient stock: {product.Name}");

                    // 7) PRICE CONTROL (manipülasyon koruması)
                    var currentPrice = product.SalePriceIncludingTaxes;
                    if (item.PriceIncludingTaxes != currentPrice)
                        return BadRequest("The price has changed, please refresh the page.");

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
                return StatusCode(500, "Order could not be created.");
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
                    return NotFound("Payment not found.");

                if (payment.Status != enmPaymentStatus.Pending)
                {
                    payment.FailedAt = DateTime.Now;
                    payment.FailureReason = "Payment is not in the pending state. It is either completed or cancelled.";

                    await _paymentService.UpdateAsync(payment);
                    await _unitOfWork.Commit();

                    return BadRequest("Payment is not in the pending state. It is either completed or cancelled.");
                }

                payment.Status = enmPaymentStatus.Paid;
                payment.PaidAt = DateTime.Now;

                await _paymentService.UpdateAsync(payment);

                // Order Update
                var order = await _baseService.GetByIdAsync(payment.OrderId);
                if (order == null)
                    return NotFound("Order not found.");

                order.Status = enmOrderStatus.Paid;

                await _baseService.UpdateAsync(order);
                await _unitOfWork.Commit();

                HttpContext.Session.Remove("CartSession");

                #region ONAY E-POSTASI
                var user = await _userManager.FindByIdAsync(order.UserId.ToString());
                
                var subject = "Your order has been successfully received";

                var body = $@"
                    <div style='font-family:Arial'>
                        <h2>Thank you {user!.FullName}!</h2>

                        <p>Your order has been successfully created and payment has been received.</p>

                        <h4>Order No: {order.OrderNumber}</h4>
                        <p>Total Amount: ₺ {order.TotalAmount:N2}</p>

                        <p>Your order has been moved to the processing stage.</p>

                        <br/>

                        <a href='https://localhost:7103/Order/Details/{order.Id}'>
                            View My Order
                        </a>

                        <br/><br/>

                        <small>AtlasCommerce</small>
                    </div>
                ";

                await _emailService.SendAsync(user!.Email, subject, body);
                #endregion

                return Ok("Your payment has been processed successfully.");
            }
            catch (Exception ex)
            {
                var payment = await _paymentService.GetByIdAsync(paymentId);

                if (payment != null)
                {
                    payment.Status = enmPaymentStatus.Failed;
                    payment.FailedAt = DateTime.Now; 
                    payment.FailureReason = $"System Error: {ex.Message}";

                    await _paymentService.UpdateAsync(payment);
                    await _unitOfWork.Commit();
                }

                return BadRequest($"An error occurred while processing the payment: {ex.Message}");
            }
        }

        // Order DETAILS
        public async Task<IActionResult> Detail(Guid id)
        {
            await LoadProductsToDropdown();
            await SetCart();

            var order = (await _baseService.GetAllAsync(i => i.Items))
                ?.FirstOrDefault(i => i.Id == id);

            if (order == null)
                return NotFound();

            var payment = (await _paymentService
                .GetAllAsync(x => x.OrderId == order.Id))
                .FirstOrDefault();

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
                }
            };

            var settings = await _settingsCacheService.GetAsync();

            if (settings == null)
            {
                var entity = (await _settingsService.GetAllAsync()).FirstOrDefault();
                settings = _mapper.Map<WebsiteSettingsVM>(entity);

                await _settingsCacheService.SetAsync(settings, TimeSpan.FromDays(1));
            }

            vm.Settings = settings;

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
                return BadRequest("This order is not eligible for payment.");

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
                return BadRequest("Only draft/pending payment orders can be deleted.");

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

                return BadRequest("Draft order could not be deleted.");
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
