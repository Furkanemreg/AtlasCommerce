using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class SaleProductVM
    {
        public Guid Id { get; set; }

        public required string Title { get; set; }

        public required string Barcode { get; set; }

        public string? Description { get; set; }

        public string? ShortDescription { get; set; }

        public decimal SalePrice { get; set; }

        public int DiscountRate { get; set; }

        public DateTime? DiscountStartAt { get; set; }

        public DateTime? DiscountEndAt { get; set; }

        public Guid CategoryId { get; set; }

        public CategoryVM? Category { get; set; }

        public decimal Weight { get; set; }

        public decimal Height { get; set; }

        public decimal Width { get; set; }

        public decimal Lenght { get; set; }

        public string? Attribute { get; set; }

        public int Color { get; set; }

        public Guid? MainImageId { get; set; }

        public ICollection<ImageVM>? Images { get; set; }
        public bool ShowInSelected { get; set; }

        [NotMapped]
        public decimal SalePriceWithDiscount
        {
            get
            {
                if (DiscountRate > 0
                    && DiscountStartAt.HasValue
                    && DiscountEndAt.HasValue
                    && DiscountStartAt.Value.Date <= DateTime.Now.Date
                    && DiscountEndAt.Value.Date >= DateTime.Now.Date)
                {
                    var discounted = SalePrice * (1 - DiscountRate / 100m);
                    return decimal.Round(discounted, 2);
                }

                return decimal.Round(SalePrice, 2);
            }
        }

        public WebsiteSettingsVM? Settings { get; set; }
    }
}
