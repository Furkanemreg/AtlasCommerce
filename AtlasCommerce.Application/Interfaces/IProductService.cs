using AtlasCommerce.Application.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Interfaces
{
    public interface IProductService
    {
        PagedResult<ProductListVM> GetPagedProducts(int pageNumber, int pageSize, string? searchTerm = null);
    }
}
