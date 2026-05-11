using AtlasCommerce.Application.Interfaces;
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
            IMemoryCache memoryCache)
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
            var currentUserId = _currentUserService.UserId();

            await LoadProductsToDropdown();
            SetCart();

            var payments = await _baseService.GetAllAsync(x => x.Order);

            payments = payments
                .Where(p => p.Order.UserId == currentUserId)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            var settingsVm = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

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
                }).ToList(),

                Settings = settingsVm
            };

            return View(vm);
        }

    }
}

