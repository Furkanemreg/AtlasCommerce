using AtlasCommerce.Application.Interfaces.Caching;
using AtlasCommerce.Application.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Persistance.Services.Caching
{
    public class CartCacheService : ICartCacheService
    {
        private readonly ICacheService _cacheService;

        public CartCacheService(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public async Task<List<CartItemVM>> GetAsync(string cartKey)
        {
            return await _cacheService.GetAsync<List<CartItemVM>>(cartKey)
                   ?? new List<CartItemVM>();
        }

        public async Task SetAsync(string cartKey, List<CartItemVM> cart, TimeSpan? expiration = null)
        {
            await _cacheService.SetAsync(
                cartKey,
                cart,
                expiration ?? TimeSpan.FromDays(7));
        }

        public async Task RemoveAsync(string cartKey)
        {
            await _cacheService.RemoveAsync(cartKey);
        }
    }
}
