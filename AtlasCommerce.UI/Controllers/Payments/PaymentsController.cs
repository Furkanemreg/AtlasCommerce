using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.Interfaces.Caching;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using AtlasCommerce.UI.Controllers.Base;
using AtlasCommerce.UI.Controllers.Products;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;

namespace AtlasCommerce.UI.Controllers
{
    public class PaymentsController : BaseController<Payment>
    {
        private readonly ILogger<PaymentsController> _logger;
        private readonly IMapper _mapper;
        private readonly IImageService _imageService;
        private readonly IBaseService<Product> _productService;
        private readonly IBaseService<Order> _orderService;
        private readonly IBaseService<Category> _categoryService;
        private readonly IBaseService<WebsiteSettings> _settingsService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMemoryCache _memoryCache;
        private readonly ICartCacheService _cartCacheService;
        private readonly IDropdownCacheService _dropdownCacheService;
        private readonly ISettingsCacheService _settingsCacheService;

        public PaymentsController(
            IBaseService<Payment> baseService,
            IUnitOfWork unitOfWork,
            IBaseService<Product> productService,
            IBaseService<Order> orderService,
            IBaseService<Category> categoryService,
            IBaseService<WebsiteSettings> settingsService,
            ICurrentUserService currentUserService,
            ILogger<PaymentsController> logger,
            IMapper mapper,
            IImageService imageService,
            IMemoryCache memoryCache,
            ICartCacheService cartCacheService,
            IDropdownCacheService dropdownCacheService,
            ISettingsCacheService settingsCacheService)
            : base(baseService, unitOfWork)
        {
            _logger = logger;
            _mapper = mapper;
            _imageService = imageService;
            _productService = productService;
            _orderService = orderService;
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

        public async Task<IActionResult> Index()
        {
            var currentUserId = _currentUserService.UserId();

            await LoadProductsToDropdown();
            await SetCart();

            var payments = await _baseService.GetAllAsync(x => x.Order);

            payments = payments
                .Where(p => p.Order.UserId == currentUserId)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            var vm = new PaymentListVM
            {
                Payments = payments.Select(p => new PaymentListItemVM
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt,
                    PaidAt = p.PaidAt,

                    CardBrand = p.CardBrand,
                    CardNumber = p.CardNumber,

                    OrderId = p.OrderId,
                    OrderNumber = p.Order?.OrderNumber,
                    OrderTotal = p.Order?.TotalAmount ?? 0
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

    }
}

