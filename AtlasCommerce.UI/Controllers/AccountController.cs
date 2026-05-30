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
            IMapper mapper)
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

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> Register() 
        {
            await LoadProductsToDropdown();
            var registerVM =  new RegisterVM();

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            registerVM.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(registerVM);
        }

        //[HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        //public async Task<IActionResult> Register(RegisterVM vm)
        //{
        //    await LoadProductsToDropdown();
        //    if (!ModelState.IsValid) return View(vm);

        //    var user = new AppUser
        //    {
        //        FirstName = vm.FirstName,
        //        LastName = vm.LastName,
        //        UserName = vm.UserName,
        //        Email = vm.Email,
        //        PhoneNumber = vm.PhoneNumber,
        //    };

        //    var create = await _userManager.CreateAsync(user, vm.Password);
        //    if (!create.Succeeded)
        //    {
        //        foreach (var e in create.Errors)
        //            ModelState.AddModelError("", e.Description);
        //        return View(vm);
        //    }

        //    // "User" rolü yoksa oluştur
        //    if (!await _roleManager.RoleExistsAsync("User"))
        //    {
        //        await _roleManager.CreateAsync(new IdentityRole<Guid>("User"));
        //    }

        //    // Kullanıcıyı "User" rolüne ata
        //    await _userManager.AddToRoleAsync(user, "User");



        //    TempData["RegisterMessage"] = "Kayıt başarılı. Giriş yapabilirsiniz.";
        //    return RedirectToAction(nameof(Login));
        //}

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
                "Hesabınızı doğrulayın",
                        $@"
                    <h3>Hoş geldiniz {user.FirstName}</h3>
                    <p>AtlasCommerce hesabınızı aktifleştirmek için aşağıdaki linke tıklayın:</p>
                    <a href='{confirmationLink}'>Hesabımı Doğrula</a>
                "
            );

            TempData["RegisterMessage"] = "Kayıt başarılı. Email adresinize doğrulama linki gönderildi.";

            return RedirectToAction(nameof(Login));
        }

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(Guid userId, string token)
        {
            await LoadProductsToDropdown();

            var vm = new EmptyVM();

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

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

        //public IActionResult Login(string? returnUrl = null)
        //    => View(new LoginVM { ReturnUrl = returnUrl });

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            await LoadProductsToDropdown();

            LoginVM loginVM = new LoginVM
            {
                ReturnUrl = returnUrl
            };

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            loginVM.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(loginVM);
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM vm)
        {
            await LoadProductsToDropdown();

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.FindByNameAsync(vm.UserName);
            if (user == null)
            {
                ModelState.AddModelError("", "Kullanıcı bulunamadı.");
                return View(vm);
            }

            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError("", "Email adresiniz doğrulanmamış. Lütfen gelen kutunuzu kontrol ediniz.");
                return View(vm);
            }

            // Rolleri kontrol et
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("User"))
            {
                ModelState.AddModelError("", "Bu hesapla giriş yapılamaz.");
                return View(vm);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, vm.Password, vm.RememberMe, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", result.IsLockedOut
                    ? "Hesap kilitlendi."
                    : "Kullanıcı adı veya şifre hatalı.");
                return View(vm);
            }

            if (user.IsActive != true)
            {
                ModelState.AddModelError("", "Hesabınız pasif durumdadır. Lütfen yönetici ile iletişime geçiniz.");
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
            SetCart();

            var vm = new EmptyVM();

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(vm);
        }

        [Authorize]
        public async Task<IActionResult> Settings()
        {
            await LoadProductsToDropdown();
            SetCart();

            var vm = new EmptyVM();

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(vm);
        }

        [Authorize]
        public async Task<IActionResult> Messages()
        {
            await LoadProductsToDropdown();
            SetCart();

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

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateUserSettings(UpdateUserSettingsDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return Unauthorized();
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
            if (!isPasswordValid)
            {
                ModelState.AddModelError("", "Mevcut şifre yanlış girildi.");
                TempData["ErrorMessage"] = "Mevcut şifre yanlış girildi.";

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

                TempData["ErrorMessage"] = "Güncelleme sırasında bir hata oluştu.";
                return RedirectToAction("Settings", "Account");
            }

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                var passwordChangeResult = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.Password);
                if (!passwordChangeResult.Succeeded)
                {
                    foreach (var error in passwordChangeResult.Errors)
                        ModelState.AddModelError("", error.Description);


                    TempData["ErrorMessage"] = "Şifre güncelleme sırasında bir hata oluştu.";
                    return RedirectToAction("Settings", "Account");
                }
            }

            TempData["SuccessMessage"] = "Hesap ayarları başarıyla güncellendi.";
            return RedirectToAction("Settings", "Account");
        }


    }
}
