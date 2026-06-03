using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class PageVM
    {
        public ICollection<DropdownCategoryVM>? DropdownCategories { get; set; }

        public ICollection<CategoryWithProductsVM>? CategoryWithProducts { get; set; }

        //public ICollection<SaleProductVM>? SelectedProducts =>
        //    CategoryWithProducts?
        //        .SelectMany(c => c.Products)
        //        .Where(p => p.ShowInSelected)
        //        .ToList();

        public List<SaleProductVM>? SelectedProducts { get; set; }

        // RECOMMENDATION SYSTEM OUTPUT
        public List<SaleProductVM>? RecommendedProducts { get; set; }
        
        public WebsiteSettingsVM? Settings { get; set; }
        public List<BannerVM>? Banners { get; set; }
    }
}
