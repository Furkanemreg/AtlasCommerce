using AutoMapper;
using AtlasCommerce.Application.DTOs;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Newtonsoft.Json;
using AtlasCommerce.Persistance.Services;
using AtlasCommerce.Application.Interfaces.Caching;

namespace AtlasCommerce.UI.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IBaseService<Category> _categoryService;
        private readonly IBaseService<Product> _productService;
        private readonly IBaseService<UserMessage> _messageService;
        private readonly IBaseService<WebsiteSettings> _settingsService;
        private readonly IImageService _imageService;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ICartCacheService _cartCacheService;
        private readonly IDropdownCacheService _dropdownCacheService;
        private readonly ISettingsCacheService _settingsCacheService;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IBaseService<Category> categoryService,
            IBaseService<Product> productService,
            IBaseService<UserMessage> messageService,
            IBaseService<WebsiteSettings> settingsService,
            IImageService imageService,
            IEmailService emailService,
            IMapper mapper,
            ICartCacheService cartCacheService,
            IDropdownCacheService dropdownCacheService,
            ISettingsCacheService settingsCacheService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _categoryService = categoryService;
            _productService = productService;
            _messageService = messageService;
            _settingsService = settingsService;
            _imageService = imageService;
            _emailService = emailService;
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

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> Register() 
        {
            await LoadProductsToDropdown();
            var registerVM =  new RegisterVM();

            var settings = await _settingsCacheService.GetAsync();

            if (settings == null)
            {
                var entity = (await _settingsService.GetAllAsync()).FirstOrDefault();
                settings = _mapper.Map<WebsiteSettingsVM>(entity);

                await _settingsCacheService.SetAsync(settings, TimeSpan.FromDays(1));
            }

            registerVM.Settings = settings;

            return View(registerVM);
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM vm)
        {
            await LoadProductsToDropdown();

            if (!ModelState.IsValid)
                return View(vm);

            var user = new AppUser
            {
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                UserName = vm.UserName,
                Email = vm.Email,
                PhoneNumber = vm.PhoneNumber,
                EmailConfirmed = false
            };

            var create = await _userManager.CreateAsync(user, vm.Password);
            if (!create.Succeeded)
            {
                foreach (var e in create.Errors)
                    ModelState.AddModelError("", e.Description);

                return View(vm);
            }

            // "User" rolü yoksa oluştur
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>("User"));
            }

            // Kullanıcıyı role ata
            await _userManager.AddToRoleAsync(user, "User");

            // =========================
            // EMAIL CONFIRMATION
            // =========================
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var confirmationLink = Url.Action(
                "ConfirmEmail",
                "Account",
                new
                {
                    userId = user.Id,
                    token = token
                },
                Request.Scheme
            );

            await _emailService.SendAsync(
                user.Email,
                "Confirm Your AtlasCommerce Account",
                $@"
                    <h3>Welcome {user.FirstName}</h3>
                    <p>Click the link below to activate your AtlasCommerce account:</p>
                    <a href='{confirmationLink}'>Verify My Account</a>
                "
            );

            TempData["RegisterMessage"] = "Registration successful. A verification link has been sent to your email address.";

            return RedirectToAction(nameof(Login));
        }

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(Guid userId, string token)
        {
            await LoadProductsToDropdown();

            var vm = new EmptyVM();

            var settings = await _settingsCacheService.GetAsync();

            if (settings == null)
            {
                var entity = (await _settingsService.GetAllAsync()).FirstOrDefault();
                settings = _mapper.Map<WebsiteSettingsVM>(entity);

                await _settingsCacheService.SetAsync(settings, TimeSpan.FromDays(1));
            }

            vm.Settings = settings;

            if (userId == Guid.Empty || string.IsNullOrEmpty(token))
                return BadRequest();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
                return View("ConfirmEmailSuccess", vm);

            return View("ConfirmEmailFailed", vm);
        }

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            await LoadProductsToDropdown();

            LoginVM loginVM = new LoginVM
            {
                ReturnUrl = returnUrl
            };

            var settings = await _settingsCacheService.GetAsync();

            if (settings == null)
            {
                var entity = (await _settingsService.GetAllAsync()).FirstOrDefault();
                settings = _mapper.Map<WebsiteSettingsVM>(entity);

                await _settingsCacheService.SetAsync(settings, TimeSpan.FromDays(1));
            }

            loginVM.Settings = settings;

            return View(loginVM);
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM vm)
        {
            await LoadProductsToDropdown();

            var settings = await _settingsCacheService.GetAsync();

            if (settings == null)
            {
                var entity = (await _settingsService.GetAllAsync()).FirstOrDefault();
                settings = _mapper.Map<WebsiteSettingsVM>(entity);

                await _settingsCacheService.SetAsync(settings, TimeSpan.FromDays(1));
            }

            vm.Settings = settings;

            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.FindByNameAsync(vm.UserName);
            if (user == null)
            {
                ModelState.AddModelError("", "Kullanıcı bulunamadı.");
                return View(vm);
            }

            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError("", "Your email address has not been verified. Please check your inbox.");
                return View(vm);
            }

            // Rolleri kontrol et
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("User"))
            {
                ModelState.AddModelError("", "You cannot sign in with this account.");
                return View(vm);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, vm.Password, vm.RememberMe, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", result.IsLockedOut
                    ? "Account has been locked."
                    : "Invalid username or password.");
                return View(vm);
            }

            if (user.IsActive != true)
            {
                ModelState.AddModelError("", "Your account is inactive. Please contact the administrator.");
                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Logout(string? returnUrl = "/")
        {
            await LoadProductsToDropdown();
            await _signInManager.SignOutAsync();
            return Redirect(returnUrl!);
        }

        [AllowAnonymous]
        public async Task<IActionResult> AccessDenied()
        {
            await LoadProductsToDropdown();
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Profile()
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

        [Authorize]
        public async Task<IActionResult> Settings()
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

        [Authorize]
        public async Task<IActionResult> Messages()
        {
            await LoadProductsToDropdown();
            await SetCart();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var messages = await _messageService.GetAllAsync(x => x.UserId == user.Id);

            var vm = new UserMessagesVM
            {
                Messages = messages
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new UserMessageItemVM
                    {
                        Id = x.Id,
                        Topic = x.Topic,
                        Message = x.Message,
                        CreatedAt = x.CreatedAt,
                        IsRead = x.IsRead,

                        AdminReply = x.AdminReply,
                        RepliedAt = x.RepliedAt,
                        RepliedBy = x.RepliedBy
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

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateUserSettings(UpdateUserSettingsDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return Unauthorized();
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
            if (!isPasswordValid)
            {
                ModelState.AddModelError("", "The current password is incorrect.");
                TempData["ErrorMessage"] = "The current password is incorrect.";

                return RedirectToAction("Settings", "Account");
            }

            user.FirstName = dto.Name;
            user.LastName = dto.LastName;
            user.UserName = dto.UserName;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                    ModelState.AddModelError("", error.Description);

                TempData["ErrorMessage"] = "An error occurred while updating.";
                return RedirectToAction("Settings", "Account");
            }

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                var passwordChangeResult = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.Password);
                if (!passwordChangeResult.Succeeded)
                {
                    foreach (var error in passwordChangeResult.Errors)
                        ModelState.AddModelError("", error.Description);

                    TempData["ErrorMessage"] = "An error occurred while updating the password.";
                    return RedirectToAction("Settings", "Account");
                }
            }

            TempData["SuccessMessage"] = "Account settings have been successfully updated.";
            return RedirectToAction("Settings", "Account");
        }
    }
}
