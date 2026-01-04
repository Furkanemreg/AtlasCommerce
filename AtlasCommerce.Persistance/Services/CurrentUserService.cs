using System.Security.Claims;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace AtlasCommerce.Persistance.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<AppUser> _userManager;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, UserManager<AppUser> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        private AppUser? GetCurrentUser()
        {
            var userPrincipal = _httpContextAccessor.HttpContext?.User;
            if (userPrincipal == null) return null;

            return _userManager.GetUserAsync(userPrincipal).GetAwaiter().GetResult();
        }

        public Guid UserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user == null
                ? Guid.Empty
                : Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
        }

        public string FirstName()
        {
            var appUser = GetCurrentUser();
            return appUser?.FirstName ?? string.Empty;
        }

        public string LastName()
        {
            var appUser = GetCurrentUser();
            return appUser?.LastName ?? string.Empty;
        }

        public string UserName()
        {
            var appUser = GetCurrentUser();
            return appUser?.UserName ?? string.Empty;
        }

        public string EmailAddress()
        {
            var appUser = GetCurrentUser();
            return appUser?.Email ?? string.Empty;
        }

        public string PhoneNumber()
        {
            var appUser = GetCurrentUser();
            return appUser?.PhoneNumber ?? string.Empty;
        }
    }
}
