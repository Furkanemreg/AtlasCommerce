using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class OrderSummaryVM
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; }

        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }

        public enmOrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
