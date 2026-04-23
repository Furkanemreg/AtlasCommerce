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
        public decimal BaseTotal => Items.Sum(x => x.PriceExcludingTaxes * x.Quantity);
        public decimal TotalAmount => Items.Sum(x => x.PriceIncludingTaxes * x.Quantity);
        public decimal TotalVAT => Items.Sum(x => x.VATAmount);
        public decimal TotalOTV => Items.Sum(x => x.OTVAmount);
        public decimal TotalTAX => TotalVAT + TotalOTV;
    }
}
