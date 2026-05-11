using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class PaymentListItemVM
    {
        public Guid Id { get; set; }

        public decimal Amount { get; set; }
        public enmPaymentStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public string? CardBrand { get; set; }
        public string? CardNumber { get; set; }

        public Guid OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public decimal OrderTotal { get; set; }
    }
}
