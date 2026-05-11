using AtlasCommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Domain.Entities
{
    public class OrderReturn : BaseEntity
    {
        public Guid OrderId { get; set; }
        public virtual Order Order { get; set; }

        public enmReturnStatus Status { get; set; }

        public string Reason { get; set; }

        public decimal RefundAmount { get; set; }

        public DateTime? RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }

        public virtual ICollection<OrderReturnItem> Items { get; set; }
    }
}
