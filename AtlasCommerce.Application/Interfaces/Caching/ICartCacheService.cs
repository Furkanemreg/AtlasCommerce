using AtlasCommerce.Application.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Interfaces.Caching
{
    public interface ICartCacheService
    {
        Task<List<CartItemVM>> GetAsync(string cartKey);

        Task SetAsync(string cartKey, List<CartItemVM> cart, TimeSpan? expiration = null);

        Task RemoveAsync(string cartKey);
    }
}
