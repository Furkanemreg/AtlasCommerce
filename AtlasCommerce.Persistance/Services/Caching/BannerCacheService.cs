using AtlasCommerce.Application.Interfaces.Caching;
using AtlasCommerce.Application.ViewModels;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AtlasCommerce.Persistance.Services.Caching
{
    public class BannerCacheService : IBannerCacheService
    {
        private readonly IDistributedCache _cache;

        private const string CacheKey = "ui:banners";

        public BannerCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<List<BannerVM>?> GetAsync()
        {
            var data = await _cache.GetStringAsync(CacheKey);

            if (string.IsNullOrEmpty(data))
                return null;

            return JsonConvert.DeserializeObject<List<BannerVM>>(data);
        }

        public async Task SetAsync(List<BannerVM> banners, TimeSpan? expiration = null)
        {
            await _cache.SetStringAsync(
                CacheKey,
                JsonConvert.SerializeObject(banners),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        expiration ?? TimeSpan.FromHours(6)
                });
        }

        public async Task RemoveAsync()
        {
            await _cache.RemoveAsync(CacheKey);
        }
    }
}