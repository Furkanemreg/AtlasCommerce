using AtlasCommerce.Application.Interfaces.Caching;
using AtlasCommerce.Application.ViewModels;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace AtlasCommerce.Persistance.Services.Caching
{
    public class CategoryPageCacheService : ICategoryPageCacheService
    {
        private readonly IDistributedCache _cache;

        private const string KeyPrefix = "category-page:";
        private const string KeyList = "category-page:keys";

        public CategoryPageCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<CategoryPageVM?> GetAsync(Guid categoryId)
        {
            var json = await _cache.GetStringAsync($"{KeyPrefix}{categoryId}");

            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<CategoryPageVM>(json);
        }

        public async Task SetAsync(Guid categoryId, CategoryPageVM model, TimeSpan expiration)
        {
            var key = $"{KeyPrefix}{categoryId}";

            var json = JsonSerializer.Serialize(model);

            await _cache.SetStringAsync(
                key,
                json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                });

            var keys = await GetKeysAsync();

            if (!keys.Contains(key))
            {
                keys.Add(key);

                await _cache.SetStringAsync(
                    KeyList,
                    JsonSerializer.Serialize(keys),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
                    });
            }
        }

        public async Task RemoveAsync(Guid categoryId)
        {
            var key = $"{KeyPrefix}{categoryId}";

            await _cache.RemoveAsync(key);

            var keys = await GetKeysAsync();

            if (keys.Remove(key))
            {
                await _cache.SetStringAsync(
                    KeyList,
                    JsonSerializer.Serialize(keys),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
                    });
            }
        }

        public async Task RemoveAllAsync()
        {
            var keys = await GetKeysAsync();

            foreach (var key in keys)
            {
                await _cache.RemoveAsync(key);
            }

            await _cache.RemoveAsync(KeyList);
        }

        private async Task<List<string>> GetKeysAsync()
        {
            var json = await _cache.GetStringAsync(KeyList);

            if (string.IsNullOrEmpty(json))
                return new List<string>();

            return JsonSerializer.Deserialize<List<string>>(json)
                   ?? new List<string>();
        }
    }
}