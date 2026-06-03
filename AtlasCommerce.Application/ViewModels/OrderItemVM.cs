using AtlasCommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class OrderItemVM
    {
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = null!;
        public string Barcode { get; set; }
        public string? ImageUrl { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal UnitPriceInclTax { get; set; }
        public int Quantity { get; set; }

        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal OTVAmount { get; set; }
        public decimal TotalPrice => SubTotal + TaxAmount + OTVAmount;
    }
}
