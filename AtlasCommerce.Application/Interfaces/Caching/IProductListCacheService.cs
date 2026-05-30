using AtlasCommerce.Application.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Interfaces.Caching
{
    public interface IProductListCacheService
    {
        Task<List<SaleProductVM>?> GetAsync();
        Task SetAsync(List<SaleProductVM> products, TimeSpan duration);
        Task RemoveAsync();
    }
}
