using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class CartItemVM
    {
        public Guid ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string Title { get; set; } = string.Empty;

        public decimal PriceExcludingTaxes { get; set; }
        public decimal PriceIncludingTaxes { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal VATAmount { get; set; } = 0;
        public decimal OTVAmount { get; set; } = 0;

        public string? ImageUrl { get; set; }
        public WebsiteSettingsVM? Settings { get; set; }
    }
}
