using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class ProductListVM
    {
        public Guid ProductId{ get; set; }

        public byte[]? Data { get; set; }

        public string? FileName { get; set; }

        public string? ImageDataUri { get; set; }

        public string? Barcode { get; set; }

        public string? ProductName { get; set; }

        public string? CategoryName { get; set; }

        public decimal PurchasePriceExcludingTaxes { get; set; }

        public decimal PurchasePriceIncludingTaxes { get; set; }

        public decimal SalePriceExcludingTaxes { get; set; }

        public decimal SalePriceIncludingTaxes { get; set; }

        public int TaxRate { get; set; }

        public int OTV { get; set; }

        public decimal DiscountRate { get; set; }

        public DateTime? DiscountStartAt { get; set; }

        public DateTime? DiscountEndAt { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string? CreatedUserName { get; set; }

        public string? CreatedRole { get; set; }

        public int TotalCount { get; set; }

        [NotMapped]
        public decimal SalePrice
        {
            get
            {
                // 1) Vergiler Hariç Satış fiyatı
                var excl = SalePriceExcludingTaxes;
                // 2) ÖTV+KDV ekle
                var incl = excl * (1 + (OTV + TaxRate) / 100m);
                // 3) İndirim uygulanıyorsa düş
                if (DiscountRate > 0
                    && DiscountStartAt <= DateTime.Now
                    && DiscountEndAt >= DateTime.Now)
                {
                    incl = incl * (1 - DiscountRate / 100m);
                }
                return decimal.Round(incl, 2);
            }
        }
    }
}
