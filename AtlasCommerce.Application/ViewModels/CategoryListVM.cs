using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class CategoryListVM
    {
        public List<CategoryVM> Categories { get; set; } = new();
        public WebsiteSettingsVM? Settings { get; set; }
    }
}
