using AtlasCommerce.Application.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Interfaces.Caching
{
    public interface ISettingsCacheService
    {
        Task<WebsiteSettingsVM?> GetAsync();

        Task SetAsync(WebsiteSettingsVM settings, TimeSpan? expiration = null);

        Task RemoveAsync();
    }
}
