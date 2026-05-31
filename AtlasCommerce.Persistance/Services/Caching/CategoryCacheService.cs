using System.Text.Json;
using AtlasCommerce.Application.Interfaces.Caching;
using AtlasCommerce.Application.ViewModels;
using Microsoft.Extensions.Caching.Distributed;

namespace AtlasCommerce.Persistance.Services.Caching
{
    public class CategoryCacheService : ICategoryCacheService
    {
        private const string CacheKey = "ui:categories";

        private readonly IDistributedCache _cache;

        public CategoryCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<CategoryListVM?> GetAsync()
        {
            var json = await _cache.GetStringAsync(CacheKey);

            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<CategoryListVM>(json);
        }

        public async Task SetAsync(CategoryListVM model, TimeSpan expiration)
        {
            var json = JsonSerializer.Serialize(model);

            await _cache.SetStringAsync(
                CacheKey,
                json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                });
        }

        public async Task RemoveAsync()
        {
            await _cache.RemoveAsync(CacheKey);
        }
    }
}