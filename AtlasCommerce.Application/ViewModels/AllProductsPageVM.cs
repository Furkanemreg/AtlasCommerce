using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class AllProductsPageVM
    {
        public List<ProductVM>? Products { get; set; }
        public ProductFilterVM? Filter { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        public List<CategoryVM>? Categories { get; set; }
        public List<Guid>? SelectedCategoryIds { get; set; }
    }

}
