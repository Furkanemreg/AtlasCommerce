using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class ProductFilterVM
    {
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public decimal? MinWidth { get; set; }
        public decimal? MaxWidth { get; set; }

        public decimal? MinHeight { get; set; }
        public decimal? MaxHeight { get; set; }

        public decimal? MinWeight { get; set; }
        public decimal? MaxWeight { get; set; }

        public decimal? MinLength { get; set; }
        public decimal? MaxLength { get; set; }

        public List<string>? SelectedColors { get; set; }
        public List<Guid>? SelectedCategories { get; set; }

        public string? Sort { get; set; }
    }

}
