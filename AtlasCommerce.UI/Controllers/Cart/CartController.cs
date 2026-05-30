using AutoMapper;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using AtlasCommerce.Persistance.Services;
using AtlasCommerce.UI.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using AtlasCommerce.Application.Interfaces.Caching;
using AtlasCommerce.Persistance.Services.Caching;

namespace AtlasCommerce.UI.Controllers.Cart
{
    [AllowAnonymous]
    public class CartController : BaseController<Product>
    {
        private readonly ILogger<CartController> _logger;
        private readonly IMapper _mapper;
        private readonly IImageService _imageService;
        private readonly IBaseService<Category> _categoryService;
        private readonly IBaseService<WebsiteSettings> _settingsService;
        private readonly ICartCacheService _cartCacheService;
        private readonly IDropdownCacheService _dropdownCacheService;
        private readonly ISettingsCacheService _settingsCacheService;

        public CartController(
            IBaseService<Product> baseService,
            IBaseService<Category> categoryService,
            IBaseService<WebsiteSettings> settingsService,
            IUnitOfWork unitOfWork,
            ILogger<CartController> logger,
            IMapper mapper,
            IImageService imageService,
            ICartCacheService cartCacheService,
            IDropdownCacheService dropdownCacheService,
            ISettingsCacheService settingsCacheService)
            : base(baseService, unitOfWork)
        {
            _categoryService = categoryService;
            _settingsService = settingsService;
            _logger = logger;
            _mapper = mapper;
            _imageService = imageService;
            _cartCacheService = cartCacheService;
            _dropdownCacheService = dropdownCacheService;
            _settingsCacheService = settingsCacheService;
        }

        // CACHING
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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await LoadProductsToDropdown();
            await SetCart();

            var items = await GetCart();

            var vm = new CartVM
            {
                Items = items
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
                var products = await _baseService
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

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromForm] Guid productId, [FromForm] int quantity = 1)
        {
            var product = await _baseService.GetByIdAsync(productId);
            if (product == null)
                return NotFound();

            var cart = await GetCart();
            var existingItem = cart.FirstOrDefault(x => x.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                string? imageDataUri = null;
                var img = await _imageService.GetByOwnerAsync(product.Id, nameof(Product));
                if (img?.Data != null)
                    imageDataUri = $"data:image/png;base64,{Convert.ToBase64String(img.Data)}";

                var basePrice = product.SalePriceExcludingTaxes;

                var otvAmount = basePrice * ((decimal)product.OTV / 100);
                var vatAmount = (basePrice + otvAmount) * ((decimal)product.TaxRate / 100);

                cart.Add(new CartItemVM
                {
                    ProductId = product.Id,
                    ProductCode = product.Barcode,
                    Title = product.Name!,
                    PriceExcludingTaxes = basePrice,
                    PriceIncludingTaxes = basePrice + otvAmount + vatAmount,
                    VATAmount = vatAmount,
                    OTVAmount = otvAmount,
                    Quantity = quantity,
                    ImageUrl = imageDataUri
                });
            }

            await SaveCart(cart);

            return Json(new { success = true, cartCount = cart.Sum(x => x.Quantity) });
        }

        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            await _cartCacheService.RemoveAsync(
                GetCartKey());

            return Json(new
            {
                success = true
            });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(Guid productId)
        {
            var cart = await _cartCacheService.GetAsync(GetCartKey());

            if (!cart.Any())
                return Json(new { success = false });

            var itemToRemove = cart.FirstOrDefault(x => x.ProductId == productId);
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                await SaveCart(cart);
            }

            var totalQuantity = cart.Sum(x => x.Quantity);
            var total = cart.Sum(x => x.PriceIncludingTaxes * x.Quantity);
            var subTotal = cart.Sum(x => x.PriceExcludingTaxes * x.Quantity);
            var totalVAT = cart.Sum(x => x.VATAmount * x.Quantity);
            var totalOTV = cart.Sum(x => x.OTVAmount * x.Quantity);
            var totalTax = totalVAT + totalOTV;

            return Json(new
            {
                success = true,
                totalQuantity,
                total,
                subTotal,
                totalVAT,
                totalOTV,
                totalTax
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(Guid productId, int quantity)
        {
            var cart = await _cartCacheService.GetAsync(GetCartKey());

            var item = cart.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                if (quantity > 0)
                    item.Quantity = quantity;
                else
                    cart.Remove(item);
            }

            await SaveCart(cart);

            var rowTotal = item != null ? item.PriceIncludingTaxes * item.Quantity : 0;
            var total = cart.Sum(x => x.PriceIncludingTaxes * x.Quantity);
            var count = cart.Sum(x => x.Quantity);
            var subTotal = cart.Sum(x => x.PriceExcludingTaxes * x.Quantity);
            var totalVAT = cart.Sum(x => x.VATAmount * x.Quantity);
            var totalOTV = cart.Sum(x => x.OTVAmount * x.Quantity);
            var totalTax = totalVAT + totalOTV;

            return Json(new
            {
                rowTotal,
                total,
                count,
                subTotal,
                totalVAT,
                totalOTV,
                totalTax
            });
        }

        private async Task<List<CartItemVM>> GetCart()
        {
            var cartKey = GetCartKey();

            var result = await _cartCacheService.GetAsync(cartKey);

            return result;
        }
        
        private async Task SaveCart(List<CartItemVM> cart)
        {
            await _cartCacheService.SetAsync(
                GetCartKey(),
                cart);
        }

        private async Task SetCart()
        {
            var cart =
                await _cartCacheService.GetAsync(GetCartKey());

            ViewBag.CartItemCount = cart.Sum(x => x.Quantity);
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var cart =
                await _cartCacheService.GetAsync(GetCartKey());

            return Json(new
            {
                count = cart.Sum(x => x.Quantity)
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetCartTotal()
        {
            var cartItems =
                await _cartCacheService.GetAsync(GetCartKey());

            var total =
                cartItems.Sum(x =>
                    x.PriceIncludingTaxes * x.Quantity);

            return Json(new
            {
                total
            });
        }
    }
}
