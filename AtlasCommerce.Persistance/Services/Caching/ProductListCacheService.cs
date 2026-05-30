using AtlasCommerce.Application.Interfaces.Caching;
using AtlasCommerce.Application.ViewModels;
using Microsoft.Extensions.Caching.Memory;

namespace AtlasCommerce.Persistance.Services.Caching
{
    public class ProductListCacheService : IProductListCacheService
    {
        private const string Key = "ui:products:all";
        private readonly IMemoryCache _cache;

        public ProductListCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<List<SaleProductVM>?> GetAsync()
        {
            _cache.TryGetValue(Key, out List<SaleProductVM>? data);
            return Task.FromResult(data);
        }

        public Task SetAsync(List<SaleProductVM> products, TimeSpan duration)
        {
            _cache.Set(Key, products, duration);
            return Task.CompletedTask;
        }

        public Task RemoveAsync()
        {
            _cache.Remove(Key);
            return Task.CompletedTask;
        }
    }
}