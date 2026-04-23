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
        public virtual Order Order { get; set; }
        public Guid OrderId { get; set; }

        public virtual Product Product { get; set; }
        public Guid ProductId { get; set; }

        // Snapshot alanlar (Product'tan kopya)
        public string ProductName { get; set; } = null!;
        public string Barcode { get; set; }

        public decimal UnitPrice { get; set; } // SalePrice INCLUDING Taxes
        public int Quantity { get; set; }

        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
