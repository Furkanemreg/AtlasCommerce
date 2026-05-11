using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class OrderListVM
    {
        public List<OrderSummaryVM> Orders { get; set; } = new();
        public WebsiteSettingsVM Settings { get; set; } = new();
    }
}
