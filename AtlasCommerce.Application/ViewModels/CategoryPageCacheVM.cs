using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class CategoryPageCacheVM
    {
        public CategoryVM Category { get; set; }

        public List<SaleProductVM> Products { get; set; }
    }
}
