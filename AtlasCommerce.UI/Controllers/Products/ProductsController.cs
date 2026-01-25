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
        public ProductsController(IBaseService<Product> baseService, IUnitOfWork unitOfWork, IBaseService<Category> categoryService, IBaseService<WebsiteSettings> settingsService,
            ILogger<ProductsController> logger, IMapper mapper, IImageService imageService)
            : base(baseService, unitOfWork)
        {
            _logger = logger;
            _mapper = mapper;
            _imageService = imageService;
            _categoryService = categoryService;
            _settingsService = settingsService;
        }

        private async Task LoadProductsToDropdown()
        {
            // Navbardaki her kategori için ürünlerini getir
            var dropdownCategories = await _categoryService.GetAllAsync(x => x.ShowInDropDown == true && x.IsActive == true);
            var categoryWithProducts = new List<CategoryWithProductsVM>();

            foreach (var cat in dropdownCategories)
            {
                var products = await _baseService.GetAllAsync(p => p.CategoryId == cat.Id && p.IsActive);
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

        [HttpGet]
        public async Task<IActionResult> View(Guid id)
        {
            await LoadProductsToDropdown();
            SetCart();

            if (id == Guid.Empty)
                throw new Exception("Ürün ID'si bulunamadı.");

            var product = await _baseService.GetByIdAsync(id);
            if (product == null)
                throw new Exception("Ürün bulunamadı.");

            product.Images = await _imageService.GetAllByOwnerAsync(id, nameof(Product));

            var vm = _mapper.Map<SaleProductVM>(product);

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

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
            await LoadProductsToDropdown();
            SetCart();

            var allCategories = await _categoryService.GetAllAsync(c => c.IsActive);
            ViewBag.AllCategories = allCategories.ToList();

            var productsQuery = await _baseService.GetAllAsync(p => p.IsActive);

            IEnumerable<Product> filteredProducts = productsQuery;

            // Sıralama
            filteredProducts = sort switch
            {
                "newest" => filteredProducts.OrderByDescending(p => p.CreatedAt),
                "priceasc" => filteredProducts.OrderBy(p => p.SalePriceIncludingTaxes),
                "pricedesc" => filteredProducts.OrderByDescending(p => p.SalePriceIncludingTaxes),
                _ => filteredProducts
            };

            // Kategoriye göre filtreleme
            if (SelectedCategoryIds != null && SelectedCategoryIds.Any())
            {
                filteredProducts = filteredProducts
                    .Where(p => SelectedCategoryIds.Contains(p.CategoryId));
            }

            // Fiyat filtreleme (indirim hesaba katılarak)
            if (minPrice.HasValue)
            {
                filteredProducts = filteredProducts.Where(p =>
                {
                    var now = DateTime.Now.Date;
                    var hasDiscount = p.DiscountRate > 0 &&
                                      p.DiscountStartAt.HasValue &&
                                      p.DiscountEndAt.HasValue &&
                                      now >= p.DiscountStartAt.Value.Date &&
                                      now <= p.DiscountEndAt.Value.Date;

                    var priceToCompare = hasDiscount
                        ? p.SalePriceIncludingTaxes * (1 - (p.DiscountRate / 100m))
                        : p.SalePriceIncludingTaxes;

                    return priceToCompare >= minPrice.Value;
                });
            }

            if (maxPrice.HasValue)
            {
                filteredProducts = filteredProducts.Where(p =>
                {
                    var now = DateTime.Now.Date;
                    var hasDiscount = p.DiscountRate > 0 &&
                                      p.DiscountStartAt.HasValue &&
                                      p.DiscountEndAt.HasValue &&
                                      now >= p.DiscountStartAt.Value.Date &&
                                      now <= p.DiscountEndAt.Value.Date;

                    var priceToCompare = hasDiscount
                        ? p.SalePriceIncludingTaxes * (1 - (p.DiscountRate / 100m))
                        : p.SalePriceIncludingTaxes;

                    return priceToCompare <= maxPrice.Value;
                });
            }

            // Diğer boyutsal filtreler
            if (minWidth.HasValue) filteredProducts = filteredProducts.Where(p => p.Width >= minWidth.Value);
            if (maxWidth.HasValue) filteredProducts = filteredProducts.Where(p => p.Width <= maxWidth.Value);
            if (minHeight.HasValue) filteredProducts = filteredProducts.Where(p => p.Height >= minHeight.Value);
            if (maxHeight.HasValue) filteredProducts = filteredProducts.Where(p => p.Height <= maxHeight.Value);
            if (minWeight.HasValue) filteredProducts = filteredProducts.Where(p => p.Weight >= minWeight.Value);
            if (maxWeight.HasValue) filteredProducts = filteredProducts.Where(p => p.Weight <= maxWeight.Value);
            if (minLength.HasValue) filteredProducts = filteredProducts.Where(p => p.Lenght >= minLength.Value);
            if (maxLength.HasValue) filteredProducts = filteredProducts.Where(p => p.Lenght <= maxLength.Value);

            // Renk filtreleme
            var selectedColorEnums = SelectedColors?
                .Select(colorStr => Enum.TryParse<enmColors>(colorStr, out var colorEnum) ? (int?)colorEnum : null)
                .Where(e => e.HasValue)
                .Select(e => e.Value)
                .ToList();

            if (SelectedColors != null && SelectedColors.Length > 0 && selectedColorEnums?.Any() == true)
            {
                filteredProducts = filteredProducts.Where(p => selectedColorEnums.Contains(p.Color)).ToList();
            }

            var productsList = filteredProducts.ToList();

            var pagedProducts = productsList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var products = pagedProducts.Select(p => new SaleProductVM
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

            var totalPages = (int)Math.Ceiling((double)productsList.Count / pageSize);
            int totalRecords = productsList.Count;

            var filterVm = new ProductFilterVM
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
            };

            var vm = new ProductListPageVM
            {
                Products = products,
                Filter = filterVm,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords
            };

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query, int pageNumber = 1, int pageSize = 16)
        {
            await LoadProductsToDropdown();
            SetCart();

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

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View("All", vm);
        }


    }
}
