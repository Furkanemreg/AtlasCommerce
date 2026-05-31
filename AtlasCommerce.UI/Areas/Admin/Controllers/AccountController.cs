using AutoMapper;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Application.Wrappers;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;

namespace AtlasCommerce.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> userManager;
        private readonly RoleManager<IdentityRole<Guid>> roleManager;
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;
        private readonly IBaseService<WebsiteSettings> _settingsService;

        public AccountController(UserManager<AppUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, IAccountService accountService, IMapper mapper, IBaseService<WebsiteSettings> settingsService)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            _accountService = accountService;
            _mapper = mapper;
            _settingsService = settingsService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
        {
            PagedResult<AppUserVM> users = await _accountService.GetAllAsync(pageNumber, pageSize, searchTerm);

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            users.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(users);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromQuery(Name = "ReturnUrl")] string returnUrl)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            ModelState.Clear();

            LoginVM loginVM = new LoginVM
            {
                ReturnUrl = returnUrl
            };

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            loginVM.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(loginVM);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            loginVM.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            if (!ModelState.IsValid)
                return View(loginVM);

            var user = await userManager.FindByNameAsync(loginVM.UserName);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View(loginVM);
            }

            // Rolleri kontrol et
            var roles = await userManager.GetRolesAsync(user);
            if (!roles.Contains("Admin"))
            {
                ModelState.AddModelError("", "You cannot sign in with this account.");
                return View(loginVM);
            }

            BaseResponse result = await _accountService.LoginAsync(loginVM);

            if (!result.Success)
            {
                ModelState.AddModelError("Error", result.Message ?? "Invalid username or password.");
                return View(loginVM);
            }

            if (user.IsActive != true)
            {
                ModelState.AddModelError("", "Your account is inactive. Please contact the administrator.");
                return View(loginVM);
            }

            return Redirect(loginVM?.ReturnUrl ?? "/Admin/Dashboard/Index");
        }

        public async Task<IActionResult> Logout([FromQuery(Name = "ReturnUrl")] string returnUrl = "/")
        {
            await _accountService.LogoutAsync();
            return Redirect(returnUrl);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id = null)
        {
            List<string?> allRoles = _accountService.GetAllRoles();
            ViewBag.RoleList = new SelectList(allRoles, allRoles);
            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();

            if (string.IsNullOrEmpty(id))
            {
                AppUserVM appUserVM = new AppUserVM();
                appUserVM.CreatedUser = await _accountService.GetByUserName(User.Identity?.Name);

                var settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);
                appUserVM.Settings = settings;

                return View(appUserVM);
            }

            AppUserVM vm = await _accountService.GetByIdAsync(id);

            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromForm] AppUserVM vm)
        {
            ModelState.Remove("Id");

            if (vm.Id != Guid.Empty)
                ModelState.Remove("Password");

            if (!ModelState.IsValid)
                return View(vm);

            BaseResponse result;

            if (vm.Id == Guid.Empty)
                result = await _accountService.AddAsync(vm);
            else
                result = await _accountService.UpdateAsync(vm);

            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");

            string sessionUser = User.Identity?.Name ?? "Admin";
            BaseResponse result = await _accountService.DeleteAsync(id, sessionUser);

            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");

            AppUserVM user = await _accountService.GetByIdAsync(id);
            ResetPasswordVM vm = _mapper.Map<ResetPasswordVM>(user);

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM vm)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                var messages = string.Join(" ", errors);
                return Json(new { success = false, message = messages });
            }

            BaseResponse result = await _accountService.ResetPasswordAsync(vm);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}
