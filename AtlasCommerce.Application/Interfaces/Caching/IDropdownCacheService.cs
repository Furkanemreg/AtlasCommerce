using AtlasCommerce.Application.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Interfaces.Caching
{
    public interface IDropdownCacheService
    {
        Task<List<CategoryWithProductsVM>?> GetAsync();

        Task SetAsync(List<CategoryWithProductsVM> data, TimeSpan? expiration = null);

        Task RemoveAsync();
    }
}
