using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class ServicePageVM
    {
        public WebsiteSettingsVM? Settings { get; set; }
        public List<ServiceVM>? Items { get; set; }
    }
}
