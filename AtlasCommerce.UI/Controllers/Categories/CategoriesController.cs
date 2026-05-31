using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using AtlasCommerce.UI.Controllers.Base;
using AtlasCommerce.UI.Controllers.Contact;
using Microsoft.AspNetCore.Mvc;
using System.Drawing.Printing;
using System;
using AutoMapper;
using AtlasCommerce.Persistance.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using AtlasCommerce.Application.Interfaces.Caching;
using AtlasCommerce.Persistance.Services.Caching;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AtlasCommerce.UI.Controllers.Categories
{
    [AllowAnonymous]
    public class CategoriesController : BaseController<Category>
    {
        private readonly ILogger<CategoriesController> _logger; 
        private readonly IMapper _mapper;
        private readonly IImageService _imageService;
        private readonly IBaseService<Category> _categoryService;
        private readonly IBaseService<Product> _productService;
        private readonly IBaseService<WebsiteSettings> _settingsService;
        private readonly ICartCacheService _cartCacheService;
        private readonly IDropdownCacheService _dropdownCacheService;
        private readonly ISettingsCacheService _settingsCacheService;
        private readonly ICategoryCacheService _categoryCacheService;
        private readonly ICategoryPageCacheService _categoryPageCacheService;

        public CategoriesController(IBaseService<Category> baseService, IUnitOfWork unitOfWork, IBaseService<Category> categoryService, IBaseService<Product> productService, IBaseService<WebsiteSettings> settingsService,
            ILogger<CategoriesController> logger, IMapper mapper, IImageService imageService, ICartCacheService cartCacheService, IDropdownCacheService dropdownCacheService,
            ISettingsCacheService settingsCacheService, ICategoryCacheService categoryCacheService, ICategoryPageCacheService categoryPageCacheService)
            : base(baseService, unitOfWork)
        {
            _logger = logger;
            _mapper = mapper;
            _imageService = imageService;
            _categoryService = categoryService;
            _productService = productService;
            _settingsService = settingsService;
            _cartCacheService = cartCacheService;
            _dropdownCacheService = dropdownCacheService;
            _settingsCacheService = settingsCacheService;
            _categoryCacheService = categoryCacheService;
            _categoryPageCacheService = categoryPageCacheService;
        }

        #region CACHING / Common Areas
        private const string CartCookieName = "cart_id";
        private const string DropdownCacheKey = "ui:dropdown:categories";
        private const string SettingsCacheKey = "ui:settings";
        private const string CategoryPageCacheKey = "ui:categories:page";
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
                        CategoryId = p.CategoryId,
                        CreatedAt = p.CreatedAt
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
        #endregion

        public async Task<IActionResult> Index()
        {
            await LoadProductsToDropdown();
            await SetCart();

            // =========================
            // CACHE HIT
            // =========================
            var cached = await _categoryCacheService.GetAsync();

            if (cached != null)
            {
                return View(cached);
            }

            // =========================
            // DROPDOWN CATEGORIES
            // =========================
            var dropdownCategories = await _baseService.GetAllAsync(i =>
                i.ShowInDropDown == true && i.IsActive == true);

            var dropdownCategoryModels = new List<DropdownCategoryVM>();

            foreach (var c in dropdownCategories.Where(c => !string.IsNullOrEmpty(c.Name)))
            {
                string? imageDataUri = null;

                var imageResult = await _imageService.GetByOwnerAsync(c.Id, nameof(Category));

                if (imageResult?.Data != null)
                {
                    imageDataUri =
                        $"data:image/png;base64,{Convert.ToBase64String(imageResult.Data)}";
                }

                dropdownCategoryModels.Add(new DropdownCategoryVM
                {
                    Id = c.Id,
                    Name = c.Name!,
                    ImageDataUri = imageDataUri
                });
            }

            ViewBag.DropdownCategories = dropdownCategoryModels;

            // =========================
            // CATEGORY LIST
            // =========================
            var allCategories = await _baseService.GetAllAsync(i => i.IsActive == true);

            var vmList = _mapper.Map<List<CategoryVM>>(allCategories);

            foreach (var vm in vmList)
            {
                var imageResult = await _imageService.GetByOwnerAsync(vm.Id, nameof(Category));

                if (imageResult?.Data != null)
                {
                    vm.ImageDataUri =
                        $"data:image/png;base64,{Convert.ToBase64String(imageResult.Data)}";
                }
            }

            var categoryListVm = new CategoryListVM
            {
                Categories = vmList,
                Settings = await GetSettingsCached()
            };

            // =========================
            // CACHE WRITE
            // =========================
            await _categoryCacheService.SetAsync(
                categoryListVm,
                TimeSpan.FromHours(2)
            );

            return View(categoryListVm);
        }

        [HttpGet]
        public async Task<IActionResult> View(
            Guid id,
            string? sort,
            decimal? minPrice,
            decimal? maxPrice,
            decimal? minWidth,
            decimal? maxWidth,
            decimal? minHeight,
            decimal? maxHeight,
            decimal? minWeight,
            decimal? maxWeight,
            decimal? minLength,
            decimal? maxLength,
            string[]? SelectedColors,
            int pageNumber = 1,
            int pageSize = 16
        )
        {
            await LoadProductsToDropdown();
            await SetCart();

            if (id == Guid.Empty)
                throw new Exception("Category ID could not be found.");

            var category = await _baseService.GetByIdAsync(id);
            if (category == null)
                throw new Exception("Category could not be found.");

            var cached = await _categoryPageCacheService.GetAsync(id);

            List<SaleProductVM> allProducts;

            if (cached?.Products != null && cached.Products.Any())
            {
                allProducts = cached.Products.ToList();
            }
            else
            {
                var products = await _productService
                    .GetAllAsync(p => p.CategoryId == id && p.IsActive);

                allProducts = new List<SaleProductVM>();

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

                    var saleProductVM = new SaleProductVM
                    {
                        Id = p.Id,
                        Title = p.Name!,
                        Barcode = p.Barcode!,
                        Description = p.Description,
                        ShortDescription = p.ShortDescription,
                        SalePrice = p.SalePriceIncludingTaxes,
                        DiscountRate = p.DiscountRate,
                        DiscountStartAt = p.DiscountStartAt,
                        DiscountEndAt = p.DiscountEndAt,
                        CategoryId = p.CategoryId,
                        Weight = p.Weight,
                        Height = p.Height,
                        Width = p.Width,
                        Lenght = p.Lenght,
                        Attribute = p.Attribute,
                        MainImageId = p.MainPhotoId,
                        ShowInSelected = p.ShowInSelected,
                        CreatedAt = p.CreatedAt,
                        Images = images
                    };

                    allProducts.Add(saleProductVM);
                }

                await _categoryPageCacheService.SetAsync(
                    id,
                    new CategoryPageVM
                    {
                        Category = null,
                        Products = allProducts
                    },
                    TimeSpan.FromHours(2));
            }

            // =========================
            // FILTERING
            // =========================
            IEnumerable<SaleProductVM> filtered = allProducts;

            filtered = sort switch
            {
                "newest" => filtered.OrderByDescending(p => p.CreatedAt),
                "priceasc" => filtered.OrderBy(p => p.SalePrice),
                "pricedesc" => filtered.OrderByDescending(p => p.SalePrice),
                _ => filtered
            };

            if (minPrice.HasValue)
                filtered = filtered.Where(p => p.SalePrice >= minPrice.Value);

            if (maxPrice.HasValue)
                filtered = filtered.Where(p => p.SalePrice <= maxPrice.Value);

            if (minWidth.HasValue)
                filtered = filtered.Where(p => p.Width >= minWidth.Value);

            if (maxWidth.HasValue)
                filtered = filtered.Where(p => p.Width <= maxWidth.Value);

            if (minHeight.HasValue)
                filtered = filtered.Where(p => p.Height >= minHeight.Value);

            if (maxHeight.HasValue)
                filtered = filtered.Where(p => p.Height <= maxHeight.Value);

            if (minWeight.HasValue)
                filtered = filtered.Where(p => p.Weight >= minWeight.Value);

            if (maxWeight.HasValue)
                filtered = filtered.Where(p => p.Weight <= maxWeight.Value);

            if (minLength.HasValue)
                filtered = filtered.Where(p => p.Lenght >= minLength.Value);

            if (maxLength.HasValue)
                filtered = filtered.Where(p => p.Lenght <= maxLength.Value);

            var selectedColorEnums = SelectedColors?
                .Select(c => Enum.TryParse<enmColors>(c, out var e) ? (int?)e : null)
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .ToList();

            if (selectedColorEnums != null && selectedColorEnums.Any())
                filtered = filtered.Where(p => selectedColorEnums.Contains(p.Color));

            var productsList = filtered.ToList();

            var pagedProducts = productsList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new CategoryPageVM
            {
                Category = new CategoryVM
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    IsActive = category.IsActive,
                    ShowInDropDown = category.ShowInDropDown,
                    ImageId = category.ImageId,
                    ImageDataUri = category.Image != null
                        ? $"data:image/{Path.GetExtension(category.Image.FileName).Trim('.')};base64,{Convert.ToBase64String(category.Image.Data)}"
                        : null
                },

                Products = pagedProducts,

                Filter = new ProductFilterVM
                {
                    MinPrice = minPrice,
                    MaxPrice = maxPrice,
                    MinWidth = minWidth,
                    MaxWidth = maxWidth,
                    MinHeight = minHeight,
                    MaxHeight = maxHeight,
                    MinWeight = minWeight,
                    MaxWeight = maxWeight,
                    MinLength = minLength,
                    MaxLength = maxLength,
                    SelectedColors = SelectedColors?.ToList(),
                    Sort = sort
                },

                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = productsList.Count,
                TotalPages = (int)Math.Ceiling((double)productsList.Count / pageSize)
            };

            vm.Settings = await GetSettingsCached();

            return View(vm);
        }
    }
}
