using AtlasCommerce.Application.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Interfaces
{
    public interface IRecommendationService
    {
        Task<List<SaleProductVM>> GetForUserAsync(Guid userId);
        Task<List<SaleProductVM>> GetForAnonymousAsync();
    }
}
