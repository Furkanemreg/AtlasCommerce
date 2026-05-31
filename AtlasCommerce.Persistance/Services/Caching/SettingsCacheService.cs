using AtlasCommerce.Application.Interfaces.Caching;
using AtlasCommerce.Application.ViewModels;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace AtlasCommerce.Persistance.Services.Caching
{
    public class SettingsCacheService : ISettingsCacheService
    {
        private readonly IDistributedCache _cache;

        private const string CacheKey = "ui:settings";

        public SettingsCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<WebsiteSettingsVM?> GetAsync()
        {
            var data = await _cache.GetStringAsync(CacheKey);

            if (string.IsNullOrEmpty(data))
                return null;

            return JsonConvert.DeserializeObject<WebsiteSettingsVM>(data);
        }

        public async Task SetAsync(WebsiteSettingsVM settings, TimeSpan? expiration = null)
        {
            await _cache.SetStringAsync(
                CacheKey,
                JsonConvert.SerializeObject(settings),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        expiration ?? TimeSpan.FromDays(1)
                });
        }

        public async Task RemoveAsync()
        {
            await _cache.RemoveAsync(CacheKey);
        }
    }
}
