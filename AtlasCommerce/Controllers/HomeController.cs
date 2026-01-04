using AutoMapper;
using Azure;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using AtlasCommerce.Persistance.Services;
using AtlasCommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Buffers.Text;
using System.Diagnostics;
using System.Net.NetworkInformation;

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

        public HomeController(IBaseService<Category> categoryService, IBaseService<Product> productService, IBaseService<WebsiteSettings> settingsService, ILogger<HomeController> logger, IMapper mapper, IImageService imageService, IBaseService<WebsiteService> serviceService, IBaseService<WebsiteBanner> bannerService)
        {
            _logger = logger;
            _categoryService = categoryService;
            _productService = productService;
            _imageService = imageService;
            _settingsService = settingsService;
            _serviceService = serviceService;
            _bannerService = bannerService;
            _mapper = mapper;
        }
        public async Task<IActionResult> About()
        {
            await LoadProductsToDropdown();
            SetCart();

            var vm = new EmptyVM();

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(vm);
        }
        public async Task<IActionResult> Services()
        {
            await LoadProductsToDropdown();
            SetCart();

            var services = await _serviceService.GetAllAsync();
            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();

            var vm = new ServicePageVM
            {
                Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity),
                Items = services.Select(s => _mapper.Map<ServiceVM>(s)).ToList()
            };

            return View(vm);
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

            // home-categories (Body => Kategori Kartlarý)
            var pageVm = new PageVM();
            pageVm.DropdownCategories = new List<DropdownCategoryVM>();

            var dropdownCategories = await _categoryService.GetAllAsync(i => i.ShowInDropDown && i.IsActive);

            foreach (var c in dropdownCategories.Where(c => !string.IsNullOrEmpty(c.Name)))
            {
                string? imageDataUri = null;
                var imageResult = await _imageService.GetByOwnerAsync(c.Id, nameof(Category));
                if (imageResult != null && imageResult.Data != null)
                    imageDataUri = $"data:image/png;base64,{Convert.ToBase64String(imageResult.Data)}";

                pageVm.DropdownCategories.Add(new DropdownCategoryVM
                {
                    Id = c.Id,
                    Name = c.Name!,
                    ImageDataUri = imageDataUri
                });
            }

            pageVm.CategoryWithProducts = new List<CategoryWithProductsVM>();


            // showcase-section (Kategori & Ürün entegrasyonu)
            var activeCategories = await _categoryService.GetAllAsync(x => x.IsActive);
            pageVm.CategoryWithProducts = new List<CategoryWithProductsVM>();

            foreach (var cat in activeCategories)
            {
                var products = await _productService.GetAllAsync(p => p.CategoryId == cat.Id && p.IsActive); // aktif ürünleri getir

                if (products.Count > 0)
                {
                    var productVMs = new List<SaleProductVM>();

                    foreach (var p in products)
                    {
                        string? productImage = null;
                        var prodImage = await _imageService.GetByOwnerAsync(p.Id, nameof(Product));
                        if (prodImage != null && prodImage.Data != null)
                            productImage = $"data:image/png;base64,{Convert.ToBase64String(prodImage.Data)}";

                        productVMs.Add(new SaleProductVM
                        {
                            Id = p.Id,
                            Title = p.Name!,
                            Barcode = p.Barcode!,
                            SalePrice = p.SalePriceIncludingTaxes,
                            DiscountRate = p.DiscountRate,
                            DiscountStartAt = p.DiscountStartAt,
                            DiscountEndAt = p.DiscountEndAt,
                            MainImageId = p.MainPhotoId,
                            Images = p.Images?
                            .Select(img => new ImageVM
                            {
                                Id = img.Id,
                                FileName = img.FileName ?? string.Empty,
                                DataUri = img.Data != null
                                    ? $"data:image/png;base64,{Convert.ToBase64String(img.Data)}"
                                    : string.Empty,
                                IsMain = (img.Id == p.MainPhotoId)
                            }).ToList(),
                            Description = p.Description,
                            ShortDescription = p.ShortDescription,
                            CategoryId = p.CategoryId,
                            ShowInSelected = p.ShowInSelected,
                        });
                    }

                    pageVm.CategoryWithProducts.Add(new CategoryWithProductsVM
                    {
                        Id = cat.Id,
                        Name = cat.Name!,
                        Description = cat.Description,
                        ImageDataUri = pageVm.DropdownCategories.FirstOrDefault(x => x.Id == cat.Id)?.ImageDataUri,
                        Products = productVMs
                    });
                }
            }

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            pageVm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            var bannersEntity = await _bannerService.GetAllAsync();
            pageVm.Banners = _mapper.Map<List<BannerVM>>(bannersEntity);

            return View(pageVm);
        }

        public async Task<IActionResult> Privacy()
        {
            await LoadProductsToDropdown();
            SetCart();

            var vm = new EmptyVM();

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

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

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(vm);
        }

        //[Route("Home/Error")]
        //public IActionResult Error()
        //{
        //    return View();
        //}
    }
}
