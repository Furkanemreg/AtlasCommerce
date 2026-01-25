using AutoMapper;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Application.Wrappers;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Drawing.Printing;

namespace AtlasCommerce.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;
        private readonly IBaseService<WebsiteSettings> _settingsService;

        public UsersController(UserManager<AppUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, IAccountService accountService, IMapper mapper, IBaseService<WebsiteSettings> settingsService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _accountService = accountService;
            _mapper = mapper;
            _settingsService = settingsService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
        {
            PagedResult<AppUserVM> users = await _accountService.GetAllUsersAsync(pageNumber, pageSize, searchTerm);
            
            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            users.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(users);
        }

        [HttpPost]
        [Route("Admin/Users/DeactivateUserAsync/{id}")]
        public async Task<IActionResult> DeactivateUserAsync(string id = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");

            try 
            {
                var user = await _accountService.GetByIdAsync(id);
                user.IsActive = false;

                await _accountService.UpdateAsync(user);
                return Json(new { success = true, message = "Kullanıcı erişimi durduruldu." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata + {ex.Message}." });
            }
        }

        [HttpPost]
        [Route("Admin/Users/ActivateUserAsync/{id}")]
        public async Task<IActionResult> ActivateUserAsync(string id = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");

            try 
            {
                var user = await _accountService.GetByIdAsync(id);
                user.IsActive = true;

                await _accountService.UpdateAsync(user);

                return Json(new { success = true, message = "Kullanıcı erişimi aktif edildi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata + {ex.Message}." });
            }
        }

    }
}
