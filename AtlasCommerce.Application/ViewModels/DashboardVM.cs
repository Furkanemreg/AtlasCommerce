using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class DashboardVM
    {
        public int TotalProductCount { get; set; }
        public int TotalOrderCount { get; set; }
        public decimal TotalSales { get; set; }

        public List<DashboardOrderVM>? RecentOrders { get; set; }

        public List<DashboardMessageVM>? DashboardMessages { get; set; }
        public WebsiteSettingsVM? Settings { get; set; }
    }

    public class DashboardMessageVM
    {
        public Guid MessageId { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Topic { get; set; }

        public bool IsRead { get; set; } = false;

        // ADMIN REPLY
        public string? AdminReply { get; set; }
        public DateTime? RepliedAt { get; set; }
        public Guid? RepliedBy { get; set; }
    }
    public class DashboardOrderVM
    {
        public Guid OrderId { get; set; }
        public string? FullName { get; set; }
        public DateTime CreatedAt { get; set; } 
        public string Status { get; set; }
        public string StatusCssClass { get; set; }
    }
}
