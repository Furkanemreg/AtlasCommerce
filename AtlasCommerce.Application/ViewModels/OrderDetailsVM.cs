using AtlasCommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class OrderDetailsVM
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; }

        public enmOrderStatus Status { get; set; }
        public enmPlatform SalesChannel { get; set; }

        public bool IsPickup { get; set; }

        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }

        public string? ShippingAddress { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? ZipCode { get; set; }

        public DateTime? PaidAt { get; set; }
        public string? PaymentId { get; set; }

        // ITEMS
        public List<OrderItemVM> Items { get; set; } = new();

        // PAYMENTS
        public List<PaymentVM> Payments { get; set; } = new();

        // RETURNS
        public List<OrderReturnVM> Returns { get; set; } = new();

        public WebsiteSettingsVM Settings { get; set; }
    }
}
