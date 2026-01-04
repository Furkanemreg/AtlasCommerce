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

        public CategoriesController(IBaseService<Category> baseService, IUnitOfWork unitOfWork, IBaseService<Category> categoryService, IBaseService<Product> productService, IBaseService<WebsiteSettings> settingsService,
            ILogger<CategoriesController> logger, IMapper mapper, IImageService imageService)
            : base(baseService, unitOfWork)
        {
            _logger = logger;
            _mapper = mapper;
            _imageService = imageService;
            _categoryService = categoryService;
            _productService = productService;
            _settingsService = settingsService;
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

            var dropdownCategories = await _baseService.GetAllAsync(i => i.ShowInDropDown == true && i.IsActive == true);

            var dropdownCategoryModels = new List<DropdownCategoryVM>();

            foreach (var c in dropdownCategories.Where(c => !string.IsNullOrEmpty(c.Name)))
            {
                string? imageDataUri = null;

                var imageResult = await _imageService.GetByOwnerAsync(c.Id, nameof(Category));
                if (imageResult != null && imageResult.Data != null)
                {
                    imageDataUri = $"data:image/png;base64,{Convert.ToBase64String(imageResult.Data)}";
                }

                dropdownCategoryModels.Add(new DropdownCategoryVM
                {
                    Id = c.Id,
                    Name = c.Name!,
                    ImageDataUri = imageDataUri
                });
            }

            ViewBag.DropdownCategories = dropdownCategoryModels;

            var allCategories = await _baseService.GetAllAsync(i => i.IsActive == true);

            var vmList = _mapper.Map<List<CategoryVM>>(allCategories);

            foreach (var vm in vmList)
            {
                var imageResult = await _imageService.GetByOwnerAsync(vm.Id, nameof(Category));
                if (imageResult?.Data != null)
                {
                    vm.ImageDataUri = $"data:image/png;base64,{Convert.ToBase64String(imageResult.Data)}";
                }
            }

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            var settingsVm = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            var categoryListVm = new CategoryListVM
            {
                Categories = vmList,
                Settings = settingsVm
            };

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
            SetCart();

            if (id == Guid.Empty)
                throw new Exception("Kategori ID'si bulunamadı.");

            var category = await _baseService.GetByIdAsync(id);
            if (category == null)
                throw new Exception("Kategori bulunamadı.");

            // Ürünleri çek
            var productsQuery = await _productService.GetAllAsync(p => p.CategoryId == id && p.IsActive);

            // Filtreleme
            IEnumerable<Product> filteredProducts = productsQuery;

            // önce sort
            filteredProducts = sort switch
            {
                "newest" => filteredProducts.OrderByDescending(p => p.CreatedAt),
                "priceasc" => filteredProducts.OrderBy(p => p.SalePriceIncludingTaxes),
                "pricedesc" => filteredProducts.OrderByDescending(p => p.SalePriceIncludingTaxes),
                _ => filteredProducts
            };

            // fiyat
            //if (minPrice.HasValue)
            //    filteredProducts = filteredProducts.Where(p => p.SalePriceIncludingTaxes >= minPrice.Value);
            //if (maxPrice.HasValue)
            //    filteredProducts = filteredProducts.Where(p => p.SalePriceIncludingTaxes <= maxPrice.Value);

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


            // width
            if (minWidth.HasValue)
                filteredProducts = filteredProducts.Where(p => p.Width >= minWidth.Value);
            if (maxWidth.HasValue)
                filteredProducts = filteredProducts.Where(p => p.Width <= maxWidth.Value);

            // height
            if (minHeight.HasValue)
                filteredProducts = filteredProducts.Where(p => p.Height >= minHeight.Value);
            if (maxHeight.HasValue)
                filteredProducts = filteredProducts.Where(p => p.Height <= maxHeight.Value);

            // weight
            if (minWeight.HasValue)
                filteredProducts = filteredProducts.Where(p => p.Weight >= minWeight.Value);
            if (maxWeight.HasValue)
                filteredProducts = filteredProducts.Where(p => p.Weight <= maxWeight.Value);

            // length
            if (minLength.HasValue)
                filteredProducts = filteredProducts.Where(p => p.Lenght >= minLength.Value);
            if (maxLength.HasValue)
                filteredProducts = filteredProducts.Where(p => p.Lenght <= maxLength.Value);

            // renk (color dizisi doluysa)
            var selectedColorEnums = SelectedColors?
                .Select(colorStr => Enum.TryParse<enmColors>(colorStr, out var colorEnum) ? (int?)colorEnum : null)
                .Where(e => e.HasValue)
                .Select(e => e.Value)
                .ToList();

            if (SelectedColors != null && SelectedColors.Length > 0)
            {
                if (selectedColorEnums != null && selectedColorEnums.Any())
                {
                    filteredProducts = filteredProducts
                        .Where(p => selectedColorEnums.Contains(p.Color))
                        .ToList();
                }
            }

            ///////////////////*****************///////////////////

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
                Images = (p.Images != null
                    ? p.Images.Select(i => new ImageVM
                    {
                        Id = i.Id,
                        FileName = i.FileName,
                        DataUri = $"data:image/{Path.GetExtension(i.FileName).Trim('.')};base64,{Convert.ToBase64String(i.Data)}"
                    }).ToList()
                    : new List<ImageVM>())
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
                Sort = sort
            };

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
    }
}
