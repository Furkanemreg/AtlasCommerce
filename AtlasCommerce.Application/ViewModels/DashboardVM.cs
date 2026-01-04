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
    }
}
