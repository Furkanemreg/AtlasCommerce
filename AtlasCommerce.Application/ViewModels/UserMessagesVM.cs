using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class UserMessagesVM
    {
        public List<UserMessageItemVM> Messages { get; set; } = new();
        public WebsiteSettingsVM Settings { get; set; }
    }
}
