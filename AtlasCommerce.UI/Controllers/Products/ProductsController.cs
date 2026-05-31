using AutoMapper;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using AtlasCommerce.Persistance.Services;
using AtlasCommerce.UI.Controllers.Base;
using AtlasCommerce.UI.Controllers.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using AtlasCommerce.Application.Interfaces.Caching;

namespace AtlasCommerce.UI.Controllers.Products
{
    [AllowAnonymous]
    public class ProductsController : BaseController<Product>
    {
        private readonly ILogger<ProductsController> _logger;
        private readonly IMapper _mapper;
        private readonly IImageService _imageService;
        private readonly IBaseService<Category> _categoryService;
        private readonly IBaseService<WebsiteSettings> _settingsService;
        private readonly ICartCacheService _cartCacheService;
        private readonly IDropdownCacheService _dropdownCacheService;
        private readonly ISettingsCacheService _settingsCacheService;
        private readonly IProductListCacheService _productListCacheService;

        public ProductsController(IBaseService<Product> baseService, IUnitOfWork unitOfWork, IBaseService<Category> categoryService, IBaseService<WebsiteSettings> settingsService,
            ILogger<ProductsController> logger, IMapper mapper, IImageService imageService, ICartCacheService cartCacheService, IDropdownCacheService dropdownCacheService,
            ISettingsCacheService settingsCacheService, IProductListCacheService productListCacheService)
            : base(baseService, unitOfWork)
        {
            _logger = logger;
            _mapper = mapper;
            _imageService = imageService;
            _categoryService = categoryService;
            _settingsService = settingsService;
            _cartCacheService = cartCacheService;
            _dropdownCacheService = dropdownCacheService;
            _settingsCacheService = settingsCacheService;
            _productListCacheService = productListCacheService;
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

        [HttpGet]
        public async Task<IActionResult> View(Guid id)
        {
            await LoadProductsToDropdown();
            await SetCart();

            if (id == Guid.Empty)
                throw new Exception("Product ID could not be found.");

            var product = await _baseService.GetByIdAsync(id);
            if (product == null)
                throw new Exception("Product could not be found.");

            product.Images = await _imageService.GetAllByOwnerAsync(id, nameof(Product));

            var vm = _mapper.Map<SaleProductVM>(product);

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

        [HttpGet]
        public async Task<IActionResult> All(
            Guid[]? SelectedCategoryIds,
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
            await _productListCacheService.RemoveAsync();
            await LoadProductsToDropdown();
            await SetCart();

            var allCategories = await _categoryService.GetAllAsync(c => c.IsActive);
            ViewBag.AllCategories = allCategories.ToList();

            var cached = await _productListCacheService.GetAsync();

            List<SaleProductVM> allProducts;

            if (cached != null && cached.Any())
            {
                allProducts = cached;
            }
            else
            {
                var products = await _baseService.GetAllAsync(p => p.IsActive);

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

                    allProducts.Add(new SaleProductVM
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
                    });
                }

                await _productListCacheService.SetAsync(allProducts, TimeSpan.FromHours(2));
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

            if (SelectedCategoryIds?.Any() == true)
                filtered = filtered.Where(p => SelectedCategoryIds.Contains(p.CategoryId));

            if (minPrice.HasValue)
                filtered = filtered.Where(p => p.SalePrice >= minPrice.Value);

            if (maxPrice.HasValue)
                filtered = filtered.Where(p => p.SalePrice <= maxPrice.Value);

            if (minWidth.HasValue) filtered = filtered.Where(p => p.Width >= minWidth.Value);
            if (maxWidth.HasValue) filtered = filtered.Where(p => p.Width <= maxWidth.Value);
            if (minHeight.HasValue) filtered = filtered.Where(p => p.Height >= minHeight.Value);
            if (maxHeight.HasValue) filtered = filtered.Where(p => p.Height <= maxHeight.Value);
            if (minWeight.HasValue) filtered = filtered.Where(p => p.Weight >= minWeight.Value);
            if (maxWeight.HasValue) filtered = filtered.Where(p => p.Weight <= maxWeight.Value);
            if (minLength.HasValue) filtered = filtered.Where(p => p.Lenght >= minLength.Value);
            if (maxLength.HasValue) filtered = filtered.Where(p => p.Lenght <= maxLength.Value);

            var selectedColorEnums = SelectedColors?
                .Select(c => Enum.TryParse<enmColors>(c, out var e) ? (int?)e : null)
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .ToList();

            if (selectedColorEnums?.Any() == true)
                filtered = filtered.Where(p => selectedColorEnums.Contains(p.Color));

            var productsList = filtered.ToList();

            var pagedProducts = productsList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new ProductListPageVM
            {
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
                    SelectedCategories = SelectedCategoryIds?.ToList(),
                    Sort = sort
                },
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)productsList.Count / pageSize),
                TotalRecords = productsList.Count
            };

            vm.Settings = await GetSettingsCached();

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query, int pageNumber = 1, int pageSize = 16)
        {
            await LoadProductsToDropdown();
            await SetCart();

            var allCategories = await _categoryService.GetAllAsync(c => c.IsActive);
            ViewBag.AllCategories = allCategories.ToList();

            var allProducts = await _baseService.GetAllAsync(p => p.IsActive);
            IEnumerable<Product> filteredProducts = allProducts;

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.Trim();
                filteredProducts = filteredProducts.Where(p =>
                    (!string.IsNullOrEmpty(p.Name) && p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(p.Description) && p.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                );
            }

            var filteredList = filteredProducts.ToList();

            var pagedProducts = filteredList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var productVMs = pagedProducts.Select(p => new SaleProductVM
            {
                Id = p.Id,
                Title = p.Name,
                Barcode = p.Barcode,
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
                Images = p.Images != null
                    ? p.Images.Select(i => new ImageVM
                    {
                        Id = i.Id,
                        FileName = i.FileName,
                        DataUri = $"data:image/{Path.GetExtension(i.FileName).Trim('.')};base64,{Convert.ToBase64String(i.Data)}"
                    }).ToList()
                    : new List<ImageVM>()
            }).ToList();

            var vm = new ProductListPageVM
            {
                Products = productVMs,
                Filter = new ProductFilterVM { },
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)filteredList.Count / pageSize),
                TotalRecords = filteredList.Count
            };

            var settings = await _settingsCacheService.GetAsync();

            if (settings == null)
            {
                var entity = (await _settingsService.GetAllAsync()).FirstOrDefault();
                settings = _mapper.Map<WebsiteSettingsVM>(entity);

                await _settingsCacheService.SetAsync(settings, TimeSpan.FromDays(1));
            }

            vm.Settings = settings;

            return View("All", vm);
        }

    }
}
