using AtlasCommerce.Application.ViewModels;

namespace AtlasCommerce.Application.Interfaces.Caching
{
    public interface ICategoryPageCacheService
    {
        Task<CategoryPageVM?> GetAsync(Guid categoryId);

        Task SetAsync(Guid categoryId, CategoryPageVM model, TimeSpan expiration);

        Task RemoveAsync(Guid categoryId);

        Task RemoveAllAsync();
    }
}