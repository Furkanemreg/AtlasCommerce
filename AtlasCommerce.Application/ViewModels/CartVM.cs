using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class CartVM
    {
        public List<CartItemVM> Items { get; set; } = new();
        public WebsiteSettingsVM? Settings { get; set; }
        public decimal TotalAmount => Items.Sum(x => x.Price * x.Quantity);
    }
}
