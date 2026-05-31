using AutoMapper;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using AtlasCommerce.Persistance.Services;
using AtlasCommerce.UI.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using AtlasCommerce.Application.Interfaces.Caching;

namespace AtlasCommerce.UI.Controllers.Contact
{
    [AllowAnonymous]
    public class ContactController : BaseController<UserMessage>
    {
        private readonly ILogger<ContactController> _logger;
        private readonly IBaseService<Category> _categoryService;
        private readonly IBaseService<Product> _productService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IImageService _imageService;
        private readonly IBaseService<WebsiteSettings> _settingsService;
        private readonly IMapper _mapper;
        private readonly ICartCacheService _cartCacheService;
        private readonly IDropdownCacheService _dropdownCacheService;
        private readonly ISettingsCacheService _settingsCacheService;

        public ContactController(IBaseService<UserMessage> baseService, IUnitOfWork unitOfWork, IBaseService<Category> categoryService, UserManager<AppUser> userManager, IBaseService<Product> productService, IImageService imageService,
        ILogger<ContactController> logger,
        IBaseService<WebsiteSettings> settingsService,
        IMapper mapper,
        ICartCacheService cartCacheService, IDropdownCacheService dropdownCacheService,
            ISettingsCacheService settingsCacheService)
            : base(baseService, unitOfWork)
        {
            _categoryService = categoryService;
            _productService = productService;
            _imageService = imageService;
            _logger = logger;
            _userManager = userManager;
            _settingsService = settingsService;
            _mapper = mapper;
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
            await LoadProductsToDropdown();
            await SetCart();

            var vm = new UserMessageVM() { EmailAddress = string.Empty, Message = string.Empty, Topic = string.Empty };

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Contact/SendMessageAsync")]
        public async Task<IActionResult> SendMessageAsync(UserMessageVM messageVM)
        {
            if (!ModelState.IsValid)
            {
                await LoadProductsToDropdown();
                await SetCart();

                return View("Index", messageVM);
            }

            try
            {
                var message = new UserMessage
                {
                    EmailAddress = "",
                    Topic = messageVM.Topic,
                    Message = messageVM.Message,
                };

                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await _userManager.GetUserAsync(User);

                    if (user == null)
                        return Unauthorized();

                    // SADECE SERVER VERİSİ
                    message.UserId = user.Id;
                    message.Name = user.FirstName;
                    message.Surname = user.LastName;
                    message.EmailAddress = user.Email;
                    message.PhoneNumber = user.PhoneNumber;
                }
                else
                {
                    // Guest User formdan gelir
                    message.Name = messageVM.Name;
                    message.Surname = messageVM.Surname;
                    message.EmailAddress = messageVM.EmailAddress;
                    message.PhoneNumber = messageVM.PhoneNumber;
                }

                await _baseService.AddAsync(message);
                await _unitOfWork.Commit();

                TempData["SuccessMessage"] = "Your message has been sent successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending the message.");
                TempData["ErrorMessage"] = "An error occurred while sending the message.";
            }

            return RedirectToAction("Index");
        }
    }
}
