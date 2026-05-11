using AtlasCommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Domain.Entities
{
    public class Order : BaseEntity
    {
        // Kullanıcı
        public Guid UserId { get; set; }

        // Sipariş Bilgisi
        public string OrderNumber { get; set; }
        // Sipariş durumu
        public enmOrderStatus Status { get; set; } = enmOrderStatus.Draft;

        // Tutarlar
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; } = 0;
        public decimal TotalAmount { get; set; }

        // Adres bilgisi (snapshot!)
        public string? ShippingAddress { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? ZipCode { get; set; }

        // Ödeme bilgisi
        public DateTime? PaidAt { get; set; }
        public enmPlatform SalesChannel { get; set; } = enmPlatform.Website;
        public string? PaymentId { get; set; }

        // Navigation
        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>(); 
        public virtual ICollection<Payment>? Payments { get; set; } = new List<Payment>();
        public virtual ICollection<OrderReturn>? Returns { get; set; } = new List<OrderReturn>();
    }
}
