using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class PaymentVM
    {
        public decimal Amount { get; set; }
        public string PaymentProvider { get; set; }
        public string Currency { get; set; }
        public enmPaymentStatus Status { get; set; }
        public DateTime PaidAt { get; set; }

        public string CardNumber { get; set; }
        public string CardBrand { get; set; }

        public DateTime? FailedAt { get; set; }
        public string? FailureReason { get; set; }
    }
}
