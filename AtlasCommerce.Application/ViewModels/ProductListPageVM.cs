using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class ProductListPageVM
    {
        public List<SaleProductVM> Products { get; set; } = new();
        public ProductFilterVM Filter { get; set; } = new();

        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        public WebsiteSettingsVM? Settings { get; set; }
    }
}
