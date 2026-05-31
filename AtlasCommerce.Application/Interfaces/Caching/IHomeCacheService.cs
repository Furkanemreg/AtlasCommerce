using AtlasCommerce.Application.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Interfaces.Caching
{
    public interface IHomeCacheService
    {
        Task<PageVM?> GetHomeAsync();

        Task SetHomeAsync(PageVM vm, TimeSpan? expiration = null);

        Task RemoveAsync();
    }
}
