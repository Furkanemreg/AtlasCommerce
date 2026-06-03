using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class OrderReturnVM
    {
        public enmReturnStatus Status { get; set; }
        public string Reason { get; set; }
        public decimal RefundAmount { get; set; }
        public DateTime? RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
