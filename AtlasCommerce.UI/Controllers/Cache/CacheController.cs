using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.Interfaces.Caching;
using AtlasCommerce.Domain.Entities;
using AtlasCommerce.Persistance.Services.Caching;
using Microsoft.AspNetCore.Mvc;

namespace AtlasCommerce.UI.Controllers
{
    public class CacheController : Controller
    {
        private readonly ICacheService _cacheService;
        private readonly ICartCacheService _cartCacheService;
        private readonly IDropdownCacheService _dropdownCacheService;
        private readonly ISettingsCacheService _settingsCacheService;
        private readonly IBannerCacheService _bannerCacheService;
        private readonly IHomeCacheService _homeCacheService;
        private readonly ICategoryCacheService _categoryCacheService;
        private readonly ICategoryPageCacheService _categoryPageCacheService;
        private readonly IProductListCacheService _productListCacheService;

        public CacheController(
            ICacheService cacheService,
            ICartCacheService cartCacheService,
            IDropdownCacheService dropdownCacheService,
            ISettingsCacheService settingsCacheService,
            IBannerCacheService bannerCacheService,
            IHomeCacheService homeCacheService,
            ICategoryCacheService categoryCacheService,
            ICategoryPageCacheService categoryPageCacheService,
            IProductListCacheService productListCacheService)
        {
            _cacheService = cacheService;
            _cartCacheService = cartCacheService;
            _dropdownCacheService = dropdownCacheService;
            _settingsCacheService = settingsCacheService;
            _bannerCacheService = bannerCacheService;
            _homeCacheService = homeCacheService;
            _categoryCacheService = categoryCacheService;
            _categoryPageCacheService = categoryPageCacheService;
            _productListCacheService = productListCacheService;
        }

        private const string CartCookieName = "cart_id";
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

        [HttpPost]
        public async Task<IActionResult> ClearAll()
        {
            await _cartCacheService.RemoveAsync(GetCartKey());
            await _dropdownCacheService.RemoveAsync();
            await _settingsCacheService.RemoveAsync();
            await _bannerCacheService.RemoveAsync();
            await _homeCacheService.RemoveAsync();
            await _categoryCacheService.RemoveAsync();
            await _categoryPageCacheService.RemoveAllAsync();
            await _productListCacheService.RemoveAsync();

            return Ok();
        }
    }
}

