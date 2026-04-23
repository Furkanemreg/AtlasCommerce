using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using AtlasCommerce.UI.Controllers.Base;
using AtlasCommerce.UI.Controllers.Products;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AtlasCommerce.UI.Controllers
{
    public class OrderController : BaseController<Order>
    {
        private readonly ILogger<OrderController> _logger;
        private readonly IMapper _mapper;
        private readonly IImageService _imageService;
        private readonly IBaseService<Product> _productService;
        private readonly IBaseService<Category> _categoryService;
        private readonly IBaseService<WebsiteSettings> _settingsService;
        private readonly ICurrentUserService _currentUserService;

        public OrderController(
            IBaseService<Order> baseService, 
            IUnitOfWork unitOfWork, 
            IBaseService<Product> productService, 
            IBaseService<Category> categoryService, 
            IBaseService<WebsiteSettings> settingsService,
            ICurrentUserService currentUserService,
            ILogger<OrderController> logger, 
            IMapper mapper, 
            IImageService imageService)
            : base(baseService, unitOfWork)
        {
            _logger = logger;
            _mapper = mapper;
            _imageService = imageService;
            _productService = productService;
            _categoryService = categoryService;
            _settingsService = settingsService;
            _currentUserService = currentUserService;
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
            var orders = await _baseService.GetAllAsync();

            return View(orders);
        }

        // 1. Session'dan Sepeti Çek
        private List<CartItemVM> GetCart()
        {
            var sessionCart = HttpContext.Session.GetString("CartSession");

            if (string.IsNullOrEmpty(sessionCart))
                return new List<CartItemVM>();

            return JsonConvert.DeserializeObject<List<CartItemVM>>(sessionCart)!;
        }

        // 2. Siparişi Kaydet (Draft)
        [HttpPost]
        public async Task<IActionResult> SaveDraft()
        {
            var cart = GetCart();

            if (!cart.Any())
                return Json(new { success = false, message = "Sepetinizde ürün bulunmuyor." });

            var order = new Order
            {
                UserId = _currentUserService.UserId(),
                Status = enmOrderStatus.Draft,
                CreatedAt = DateTime.Now,
                Items = new List<OrderItem>()
            };

            foreach (var item in cart)
            {
                var product = await _productService.GetByIdAsync(item.ProductId);
                if (product == null) continue;

                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Barcode = product.Barcode,
                    UnitPrice = product.SalePriceIncludingTaxes,
                    Quantity = item.Quantity
                });
            }

            order.SubTotal = order.Items.Sum(x => x.TotalPrice);
            order.TotalAmount = order.SubTotal;

            await _baseService.AddAsync(order);
            await _unitOfWork.Commit();

            HttpContext.Session.Remove("CartSession");

            return Json(new { success = true });
        }

        // 3. Checkout => Order oluştur (Pending => Paid akışı başlangıcı)
        [HttpPost]
        public async Task<IActionResult> CreateOrderForPayment()
        {
            var cart = GetCart();

            if (!cart.Any())
                return RedirectToAction("Index", "Cart");

            var order = new Order
            {
                UserId = Guid.Empty,
                Status = enmOrderStatus.PendingPayment,
                CreatedAt = DateTime.Now,
                Items = new List<OrderItem>()
            };

            foreach (var item in cart)
            {
                var product = await _productService.GetByIdAsync(item.ProductId);

                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Barcode = product.Barcode,
                    UnitPrice = product.SalePriceIncludingTaxes,
                    Quantity = item.Quantity
                });
            }

            order.SubTotal = order.Items.Sum(x => x.TotalPrice);
            order.TotalAmount = order.SubTotal;

            await _baseService.AddAsync(order);
            await _unitOfWork.Commit();

            return RedirectToAction("Payment", new { orderId = order.Id });
        }

        // 4. Ödeme başarılı => Order güncelle
        [HttpPost]
        public async Task<IActionResult> CompletePayment(Guid orderId, string paymentId)
        {
            var order = await _baseService.GetByIdAsync(orderId);

            if (order == null)
                return NotFound();

            order.Status = enmOrderStatus.Paid;
            order.PaymentId = paymentId;
            order.PaidAt = DateTime.Now;

            await _baseService.UpdateAsync(order);
            await _unitOfWork.Commit();

            // sepet temizle
            HttpContext.Session.Remove("CartSession");

            return RedirectToAction("Success");
        }

    }
}
