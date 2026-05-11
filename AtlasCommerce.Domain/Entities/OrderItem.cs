using AtlasCommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Domain.Entities
{
    public class OrderItem : BaseEntity
    {
        public Guid OrderId { get; set; }
        public virtual Order Order { get; set; }

        public Guid ProductId { get; set; }
        public virtual Product Product { get; set; }

        // SNAPSHOT
        public string ProductName { get; set; } = null!;
        public string Barcode { get; set; }

        public decimal UnitPriceInclTax { get; set; } // For UI
        public decimal UnitPriceExclTax { get; set; } // For Calculation

        // TAX
        public int TaxRate { get; set; }   // KDV
        public int OTVRate { get; set; }

        public int Quantity { get; set; }

        // CALCULATIONS
        public decimal TaxAmount =>
            SubTotal * TaxRate / 100;

        public decimal OTVAmount =>
            SubTotal * OTVRate / 100;

        public decimal SubTotal => UnitPriceExclTax * Quantity;
        
        public decimal TotalPrice =>
            UnitPriceInclTax * Quantity;
    }
}
