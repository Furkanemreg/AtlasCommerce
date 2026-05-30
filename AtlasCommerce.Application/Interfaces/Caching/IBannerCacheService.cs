using AtlasCommerce.Application.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Interfaces.Caching
{
    public interface IBannerCacheService
    {
        Task<List<BannerVM>?> GetAsync();

        Task SetAsync(List<BannerVM> banners, TimeSpan? expiration = null);

        Task RemoveAsync();
    }
}
