using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.Interfaces.Caching;
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
        private readonly ICartCacheService _cartCacheService;
        private readonly IDropdownCacheService _dropdownCacheService;
        private readonly ISettingsCacheService _settingsCacheService;

        public CheckoutController(
                ILogger<CheckoutController> logger,
                IMapper mapper,
                ICurrentUserService currentUserService,
                IImageService imageService, 
                IBaseService<Product> productService, 
                IBaseService<Category> categoryService,
                IBaseService<Order> orderService,
                IBaseService<Payment> paymentService,
                IBaseService<WebsiteSettings> settingsService,
                ICartCacheService cartCacheService,
                IDropdownCacheService dropdownCacheService,
                ISettingsCacheService settingsCacheService)
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
            _cartCacheService = cartCacheService;
            _dropdownCacheService = dropdownCacheService;
            _settingsCacheService = settingsCacheService;
        }

        private async Task<List<CartItemVM>> GetCart()
        {
            var cartKey = GetCartKey();

            var result = await _cartCacheService.GetAsync(cartKey);

            return result;
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

        public async Task<IActionResult> Index()
        {
            await LoadProductsToDropdown();
            await SetCart();

            var cart = await GetCart();

            if (!cart.Any())
                return RedirectToAction("Index", "Cart");

            var vm = new CheckoutVM
            {
                Items = cart,
                SubTotal = cart.Sum(x => x.PriceExcludingTaxes * x.Quantity),
                VatTotal = cart.Sum(x => x.VATAmount * x.Quantity),
                OtvTotal = cart.Sum(x => x.OTVAmount * x.Quantity),
                Total = cart.Sum(x => x.PriceIncludingTaxes * x.Quantity)
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

        public async Task<IActionResult> FromDraft(Guid orderId, Guid paymentId)
        {
            await LoadProductsToDropdown();
            await SetCart();

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
            await SetCart();

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
