using AtlasCommerce.Application.Interfaces.Caching;
using AtlasCommerce.Application.ViewModels;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace AtlasCommerce.Persistance.Services.Caching
{
    public class HomeCacheService : IHomeCacheService
    {
        private readonly IDistributedCache _cache;
        private const string Key = "ui:home:page";

        public HomeCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<PageVM?> GetHomeAsync()
        {
            var data = await _cache.GetStringAsync(Key);

            return string.IsNullOrEmpty(data)
                ? null
                : JsonConvert.DeserializeObject<PageVM>(data);
        }

        public async Task SetHomeAsync(PageVM vm, TimeSpan? expiration = null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(2)
            };

            await _cache.SetStringAsync(Key, JsonConvert.SerializeObject(vm), options);
        }

        public async Task RemoveAsync()
        {
            await _cache.RemoveAsync(Key);
        }
    }
}