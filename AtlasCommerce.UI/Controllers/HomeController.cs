using AutoMapper;
using Azure;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using AtlasCommerce.Persistance.Services;
using AtlasCommerce.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Buffers.Text;
using System.Diagnostics;
using System.Net.NetworkInformation;
using AtlasCommerce.Application.Interfaces.Caching;
using AtlasCommerce.Persistance.Services.Caching;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AtlasCommerce.UI.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IBaseService<Category> _categoryService;
        private readonly IBaseService<Product> _productService;
        private readonly IBaseService<WebsiteSettings> _settingsService;
        private readonly IBaseService<WebsiteService> _serviceService;
        private readonly IBaseService<WebsiteBanner> _bannerService;
        private readonly IImageService _imageService;
        private readonly IMapper _mapper;
        private readonly ICartCacheService _cartCacheService;
        private readonly IDropdownCacheService _dropdownCacheService;
        private readonly ISettingsCacheService _settingsCacheService; 
        private readonly IBannerCacheService _bannerCacheService;
        private readonly IHomeCacheService _homeCacheService;
        private readonly IRecommendationService _recommendationService;

        public HomeController(IBaseService<Category> categoryService, 
            IBaseService<Product> productService, 
            IBaseService<WebsiteSettings> settingsService, 
            ILogger<HomeController> logger, 
            IMapper mapper, 
            IImageService imageService, 
            IBaseService<WebsiteService> serviceService, 
            IBaseService<WebsiteBanner> bannerService,
            ICartCacheService cartCacheService,
            IDropdownCacheService dropdownCacheService,
            ISettingsCacheService settingsCacheService,
            IBannerCacheService bannerCacheService,
            IHomeCacheService homeCacheService,
            IRecommendationService recommendationService)
        {
            _logger = logger;
            _categoryService = categoryService;
            _productService = productService;
            _imageService = imageService;
            _settingsService = settingsService;
            _serviceService = serviceService;
            _bannerService = bannerService;
            _mapper = mapper;
            _cartCacheService = cartCacheService;
            _dropdownCacheService = dropdownCacheService;
            _settingsCacheService = settingsCacheService;
            _bannerCacheService = bannerCacheService;
            _homeCacheService = homeCacheService;
            _recommendationService = recommendationService;
        }

        #region CACHING / Common Areas
        private const string CartCookieName = "cart_id"; 
        private const string DropdownCacheKey = "ui:dropdown:categories"; 
        private const string SettingsCacheKey = "ui:settings";
        private const string BannersCacheKey = "ui:banners";
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
        
        private async Task<WebsiteSettingsVM> GetSettingsCached()
        {
            var cached = await _settingsCacheService.GetAsync();
            if (cached != null) return cached;

            var entity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            var vm = _mapper.Map<WebsiteSettingsVM>(entity);

            await _settingsCacheService.SetAsync(vm, TimeSpan.FromDays(1));
            return vm;
        }
        private async Task<List<BannerVM>> GetBannersCached()
        {
            var cached = await _bannerCacheService.GetAsync();
            if (cached != null) return cached;

            var entities = await _bannerService.GetAllAsync();
            var vm = _mapper.Map<List<BannerVM>>(entities);

            await _bannerCacheService.SetAsync(vm, TimeSpan.FromHours(6));
            return vm;
        }
        #endregion

        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            await SetCart();

            var cached = await _homeCacheService.GetHomeAsync();

            if (cached != null)
            {
                cached.Settings ??= await GetSettingsCached();
                cached.Banners ??= await GetBannersCached();

                // Akýllý Ürün Öneri Sistemi
                if (Guid.TryParse(userIdStr, out var uID))
                {
                    cached.RecommendedProducts = await _recommendationService.GetForUserAsync(uID);
                }
                else
                {
                    cached.RecommendedProducts = await _recommendationService.GetForAnonymousAsync();
                }

                ViewBag.CategoryWithProducts = cached.CategoryWithProducts;

                return View(cached);
            }

            var pageVm = new PageVM
            {
                DropdownCategories = new List<DropdownCategoryVM>(),
                CategoryWithProducts = new List<CategoryWithProductsVM>(),
                SelectedProducts = new List<SaleProductVM>()
            };

            var dropdownCategories = await _categoryService
                .GetAllAsync(i => i.ShowInDropDown && i.IsActive);

            foreach (var c in dropdownCategories.Where(x => !string.IsNullOrEmpty(x.Name)))
            {
                var img = await _imageService.GetByOwnerAsync(c.Id, nameof(Category));

                pageVm.DropdownCategories.Add(new DropdownCategoryVM
                {
                    Id = c.Id,
                    Name = c.Name!,
                    ImageDataUri = img?.Data != null
                        ? $"data:image/png;base64,{Convert.ToBase64String(img.Data)}"
                        : null
                });
            }

            var activeCategories = await _categoryService.GetAllAsync(x => x.IsActive);

            foreach (var cat in activeCategories)
            {
                var products = await _productService
                    .GetAllAsync(p => p.CategoryId == cat.Id && p.IsActive);

                if (products == null || !products.Any())
                    continue;

                var productVMs = new List<SaleProductVM>();

                foreach (var p in products)
                {
                    var productImages = await _imageService
                        .GetAllByOwnerAsync(p.Id, nameof(Product));

                    var images = productImages.Select(img => new ImageVM
                    {
                        Id = img.Id,
                        FileName = img.FileName ?? "",
                        DataUri = img.Data != null
                            ? $"data:image/png;base64,{Convert.ToBase64String(img.Data)}"
                            : null,
                        IsMain = img.Id == p.MainPhotoId
                    }).ToList();

                    var vm = new SaleProductVM
                    {
                        Id = p.Id,
                        Title = p.Name!,
                        Barcode = p.Barcode!,
                        SalePrice = p.SalePriceIncludingTaxes,
                        DiscountRate = p.DiscountRate,
                        DiscountStartAt = p.DiscountStartAt,
                        DiscountEndAt = p.DiscountEndAt,
                        MainImageId = p.MainPhotoId,
                        Images = images,
                        Description = p.Description,
                        ShortDescription = p.ShortDescription,
                        CategoryId = p.CategoryId,
                        ShowInSelected = p.ShowInSelected
                    };

                    productVMs.Add(vm);

                    if (vm.ShowInSelected)
                        pageVm.SelectedProducts.Add(vm);
                }

                pageVm.CategoryWithProducts.Add(new CategoryWithProductsVM
                {
                    Id = cat.Id,
                    Name = cat.Name!,
                    Description = cat.Description,
                    ImageDataUri = pageVm.DropdownCategories
                        .FirstOrDefault(x => x.Id == cat.Id)?.ImageDataUri,
                    Products = productVMs
                });
            }

            pageVm.Settings = await GetSettingsCached();
            pageVm.Banners = await GetBannersCached();

            // Akýllý Ürün Öneri Sistemi
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userId, out var uid))
            {
                pageVm.RecommendedProducts = await _recommendationService.GetForUserAsync(uid);
            }
            else
            {
                pageVm.RecommendedProducts = await _recommendationService.GetForAnonymousAsync();
            }

            await _homeCacheService.SetHomeAsync(pageVm, TimeSpan.FromHours(2));

            ViewBag.CategoryWithProducts = pageVm.CategoryWithProducts;

            return View(pageVm);
        }
        public async Task<IActionResult> About()
        {
            await LoadProductsToDropdown();
            await SetCart();

            var vm = new EmptyVM();

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
        
        public async Task<IActionResult> Services()
        {
            await LoadProductsToDropdown();
            await SetCart();

            var services = await _serviceService.GetAllAsync();

            var vm = new ServicePageVM
            {
                Items = services.Select(s => _mapper.Map<ServiceVM>(s)).ToList()
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

        public async Task<IActionResult> Privacy()
        {
            await LoadProductsToDropdown();
            await SetCart();

            var vm = new EmptyVM();

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

        [Route("Home/Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Error(string message)
        {
            var vm = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                Message = message
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
