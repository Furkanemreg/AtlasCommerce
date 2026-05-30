using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AtlasCommerce.UI.Controllers.Checkout
{
    public class CheckoutController : Controller
    {
        private readonly ILogger<CheckoutController> _logger;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IImageService _imageService;
        private readonly IBaseService<Product> _productService;
        private readonly IBaseService<Category> _categoryService;
        private readonly IBaseService<Order> _orderService;
        private readonly IBaseService<Payment> _paymentService;
        private readonly IBaseService<WebsiteSettings> _settingsService;

        public CheckoutController(
                ILogger<CheckoutController> logger,
                IMapper mapper,
                ICurrentUserService currentUserService,
                IImageService imageService, 
                IBaseService<Product> productService, 
                IBaseService<Category> categoryService,
                IBaseService<Order> orderService,
                IBaseService<Payment> paymentService,
                IBaseService<WebsiteSettings> settingsService)
        {
            _logger = logger;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _imageService = imageService;
            _productService = productService;
            _categoryService = categoryService;
            _orderService = orderService;
            _paymentService = paymentService;
            _settingsService = settingsService;
        }

        private List<CartItemVM> GetCart()
        {
            var sessionCart = HttpContext.Session.GetString("CartSession");

            if (string.IsNullOrEmpty(sessionCart))
                return new List<CartItemVM>();

            return JsonConvert.DeserializeObject<List<CartItemVM>>(sessionCart)!;
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
            await LoadProductsToDropdown();
            SetCart();

            var cart = GetCart();

            if (!cart.Any())
                return RedirectToAction("Index", "Cart");

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            var settingsVm = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            var vm = new CheckoutVM
            {
                Items = cart,
                SubTotal = cart.Sum(x => x.PriceExcludingTaxes * x.Quantity),
                VatTotal = cart.Sum(x => x.VATAmount * x.Quantity),
                OtvTotal = cart.Sum(x => x.OTVAmount * x.Quantity),
                Total = cart.Sum(x => x.PriceIncludingTaxes * x.Quantity),
                Settings = settingsVm
            };

            return View(vm);
        }

        public async Task<IActionResult> FromDraft(Guid orderId, Guid paymentId)
        {
            await LoadProductsToDropdown();
            SetCart();

            var order = await _orderService.GetByIdAsync(orderId);

            if (order == null)
                return RedirectToAction("Index", "Order");

            var vm = new CheckoutVM
            {
                OrderId = order.Id,
                PaymentId = paymentId,
                Items = order.Items.Select(x => new CartItemVM
                {
                    ProductId = x.ProductId,
                    Title = x.ProductName,
                    Quantity = x.Quantity,
                    PriceIncludingTaxes = x.UnitPriceInclTax,
                    //ImageUrl = x.ImageUrl
                }).ToList(),

                SubTotal = order.SubTotal,
                //VatTotal = order.VatTotal,
                //OtvTotal = order.OtvTotal,
                Total = order.TotalAmount
            };

            return View("Index", vm);
        }

        [HttpGet]
        public async Task<IActionResult> FromOrder(Guid orderId, Guid paymentId)
        {
            await LoadProductsToDropdown();
            SetCart();

            var order = (await _orderService.GetAllAsync(x => x.Items))
                ?.FirstOrDefault(x => x.Id == orderId);

            if (order == null)
                return NotFound();

            var payment = await _paymentService.GetByIdAsync(paymentId);
            if (payment == null)
                return NotFound();

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            var settingsVm = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            var items = order.Items.Select(x => new CartItemVM
            {
                ProductId = x.ProductId,
                Title = x.ProductName,
                ProductCode = x.Barcode,
                Quantity = x.Quantity,

                PriceExcludingTaxes = x.UnitPriceExclTax,
                PriceIncludingTaxes = x.UnitPriceInclTax,

                VATAmount = x.TaxAmount,
                OTVAmount = x.OTVAmount,
                ImageUrl = x.ImageUrl,
                Settings = settingsVm
            }).ToList();

            var vm = new CheckoutVM
            {
                Items = items,

                SubTotal = items.Sum(x => x.PriceExcludingTaxes * x.Quantity),
                VatTotal = items.Sum(x => x.VATAmount),
                OtvTotal = items.Sum(x => x.OTVAmount * x.Quantity),
                Total = items.Sum(x => x.PriceIncludingTaxes * x.Quantity),

                Settings = settingsVm,

                OrderId = order.Id,
                PaymentId = payment.Id,
                OrderNumber = order.OrderNumber
            };

            return View("Index", vm);
        }
    }
}
