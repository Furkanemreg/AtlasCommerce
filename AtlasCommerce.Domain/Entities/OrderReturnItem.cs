using AtlasCommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Domain.Entities
{
    public class OrderReturnItem : BaseEntity
    {
        public Guid OrderReturnId { get; set; }
        public virtual OrderReturn OrderReturn { get; set; }

        public Guid OrderItemId { get; set; }
        public virtual OrderItem OrderItem { get; set; }

        public int Quantity { get; set; }
        public decimal RefundAmount { get; set; }
    }
}
