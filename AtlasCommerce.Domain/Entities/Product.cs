using AtlasCommerce.Domain.Common;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; set; }

        public string Barcode { get; set; }

        public string? Description { get; set; }
        
        public string? ShortDescription { get; set; }

        public decimal PurchasePriceExcludingTaxes { get; set; }

        public decimal SalePriceExcludingTaxes { get; set; }

        public int TaxRate { get; set; }

        public int OTV { get; set; }

        public decimal PurchasePriceIncludingTaxes { get; set; }

        public decimal SalePriceIncludingTaxes { get; set; }

        public int DiscountRate { get; set; }

        public DateTime? DiscountStartAt { get; set; }

        public DateTime? DiscountEndAt { get; set; }

        public decimal StockQuantity { get; set; }

        public bool StockTracking { get; set; }

        public decimal? CriticalStockLevel { get; set; }

        public Category? Category { get; set; }

        public Guid CategoryId { get; set; }

        public bool ShowInSelected { get; set; }

        public decimal Weight { get; set; }

        public decimal Lenght { get; set; }

        public decimal Width { get; set; }

        public decimal Height { get; set; }

        public string? Attribute { get; set; }

        public int Color { get; set; }

        public ICollection<Image> Images { get; set; }

        public Guid MainPhotoId { get; set; }

        public bool IsActive { get; set; }
    }
}
