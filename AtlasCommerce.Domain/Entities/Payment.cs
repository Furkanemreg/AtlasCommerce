using AtlasCommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public Guid OrderId { get; set; }
        public virtual Order Order { get; set; }

        public string PaymentProvider { get; set; } = "Unknown";
        public Guid TransactionId { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "TRY";

        public enmPaymentStatus Status { get; set; }

        public string CardNumber { get; set; }
        public string CardBrand { get; set; }

        public DateTime PaidAt { get; set; }
        
        public DateTime? FailedAt { get; set; }
        public string? FailureReason { get; set; }
    }
}
