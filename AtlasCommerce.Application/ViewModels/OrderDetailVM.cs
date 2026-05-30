using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class OrderDetailVM
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; }
        public bool IsPickup { get; set; }

        public string Address { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string ZipCode { get; set; }

        public decimal TotalTAX { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalAmount { get; set; }

        public enmOrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public PaymentInfoVM? Payment { get; set; }

        public List<OrderItemVM> Items { get; set; } = new();
        public WebsiteSettingsVM Settings { get; set; } = new();
    }
}
