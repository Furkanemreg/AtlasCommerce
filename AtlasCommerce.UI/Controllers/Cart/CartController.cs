using AutoMapper;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using AtlasCommerce.Persistance.Services;
using AtlasCommerce.UI.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

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

        public CartController(
            IBaseService<Product> baseService,
            IBaseService<Category> categoryService,
            IBaseService<WebsiteSettings> settingsService,
            IUnitOfWork unitOfWork,
            ILogger<CartController> logger,
            IMapper mapper,
            IImageService imageService)
            : base(baseService, unitOfWork)
        {
            _categoryService = categoryService;
            _settingsService = settingsService;
            _logger = logger;
            _mapper = mapper;
            _imageService = imageService;
        }

        private const string CartSessionKey = "CartSession";

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await LoadProductsToDropdown();
            SetCart();

            var items = await GetCart();

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            var vm = new CartVM
            {
                Items = items,
                Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity)
            };

            return View(vm);
        }

        private async Task LoadProductsToDropdown()
        {
            var dropdownCategories = await _categoryService.GetAllAsync(x => x.ShowInDropDown && x.IsActive);
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
            var sessionCart = HttpContext.Session.GetString(CartSessionKey);
            ViewBag.CartItemCount = !string.IsNullOrEmpty(sessionCart)
                ? JsonConvert.DeserializeObject<List<CartItemVM>>(sessionCart)!.Sum(x => x.Quantity)
                : 0;
        }

        private async Task<List<CartItemVM>> GetCart()
        {
            await LoadProductsToDropdown();
            SetCart();

            var sessionCart = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(sessionCart))
                return new List<CartItemVM>();

            return JsonConvert.DeserializeObject<List<CartItemVM>>(sessionCart)!;
        }

        [HttpGet]
        public IActionResult GetCartCount()
        {
            var cart = HttpContext.Session.GetString(CartSessionKey);
            int count = !string.IsNullOrEmpty(cart)
                ? JsonConvert.DeserializeObject<List<CartItemVM>>(cart)!.Sum(x => x.Quantity)
                : 0;

            return Json(new { count });
        }

        private void SaveCart(List<CartItemVM> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, JsonConvert.SerializeObject(cart));
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
                    Title = product.Name!,
                    PriceExcludingTaxes = basePrice,
                    PriceIncludingTaxes = basePrice + otvAmount + vatAmount,
                    VATAmount = vatAmount,
                    OTVAmount = otvAmount,
                    Quantity = quantity,
                    ImageUrl = imageDataUri
                });
            }

            SaveCart(cart);

            return Json(new { success = true, cartCount = cart.Sum(x => x.Quantity) });
        }

        [HttpPost]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CartSessionKey);

            return Json(new
            {
                success = true
            });
        }

        [HttpPost]
        public IActionResult Remove(Guid productId)
        {
            var sessionCart = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(sessionCart))
                return Json(new { success = false });

            var cart = JsonConvert.DeserializeObject<List<CartItemVM>>(sessionCart);
            var itemToRemove = cart.FirstOrDefault(x => x.ProductId == productId);
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                HttpContext.Session.SetString(CartSessionKey, JsonConvert.SerializeObject(cart));
            }

            var totalQuantity = cart.Sum(x => x.Quantity);
            var totalPrice = cart.Sum(x => x.PriceIncludingTaxes * x.Quantity);

            return Json(new { success = true, totalQuantity, totalPrice });
        }

        [HttpPost]
        public IActionResult UpdateQuantity(Guid productId, int quantity)
        {
            var sessionCart = HttpContext.Session.GetString(CartSessionKey);
            var cart = !string.IsNullOrEmpty(sessionCart)
                ? JsonConvert.DeserializeObject<List<CartItemVM>>(sessionCart)!
                : new List<CartItemVM>();

            var item = cart.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                if (quantity > 0)
                    item.Quantity = quantity;
                else
                    cart.Remove(item);
            }

            HttpContext.Session.SetString(CartSessionKey, JsonConvert.SerializeObject(cart));

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

        [HttpGet]
        public IActionResult GetCartTotal()
        {
            var sessionCart = HttpContext.Session.GetString(CartSessionKey);
            var cartItems = string.IsNullOrEmpty(sessionCart)
                ? new List<CartItemVM>()
                : JsonConvert.DeserializeObject<List<CartItemVM>>(sessionCart);

            var total = cartItems?.Sum(x => x.PriceIncludingTaxes * x.Quantity) ?? 0;

            return Json(new { total });
        }
    }
}
