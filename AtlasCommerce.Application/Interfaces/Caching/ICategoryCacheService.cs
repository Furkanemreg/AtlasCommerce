using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AtlasCommerce.Application.ViewModels;

namespace AtlasCommerce.Application.Interfaces.Caching
{
    public interface ICategoryCacheService
    {
        Task<CategoryListVM?> GetAsync();

        Task SetAsync(CategoryListVM model, TimeSpan expiration);

        Task RemoveAsync();
    }
}