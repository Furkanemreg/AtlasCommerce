using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using AtlasCommerce.Application.Interfaces.Caching;

namespace AtlasCommerce.Persistance.Services.Caching;

public sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var data = await _cache.GetStringAsync(key);

        if (string.IsNullOrWhiteSpace(data))
            return default;

        return JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null)
    {
        var options = new DistributedCacheEntryOptions();

        if (expiration.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiration;

        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(value),
            options);
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _cache.GetAsync(key) != null;
    }
}
