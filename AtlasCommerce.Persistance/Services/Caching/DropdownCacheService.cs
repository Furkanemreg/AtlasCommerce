using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using AtlasCommerce.Application.Interfaces.Caching;
using AtlasCommerce.Application.ViewModels;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace AtlasCommerce.Persistance.Services.Caching
{
    public class DropdownCacheService : IDropdownCacheService
    {
        private readonly IDistributedCache _cache;

        private const string CacheKey = "ui:dropdown:categories";

        public DropdownCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<List<CategoryWithProductsVM>?> GetAsync()
        {
            var data = await _cache.GetStringAsync(CacheKey);

            if (string.IsNullOrEmpty(data))
                return null;

            return JsonConvert.DeserializeObject<List<CategoryWithProductsVM>>(data);
        }

        public async Task SetAsync(List<CategoryWithProductsVM> data, TimeSpan? expiration = null)
        {
            await _cache.SetStringAsync(
                CacheKey,
                JsonConvert.SerializeObject(data),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        expiration ?? TimeSpan.FromHours(2)
                });
        }

        public async Task RemoveAsync()
        {
            await _cache.RemoveAsync(CacheKey);
        }
    }
}
