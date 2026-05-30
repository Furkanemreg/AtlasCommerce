using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class UserMessageItemVM
    {
        public Guid Id { get; set; }
        public string Topic { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }

        // ADMIN REPLY
        public string? AdminReply { get; set; }
        public DateTime? RepliedAt { get; set; }
        public Guid? RepliedBy { get; set; }
    }
}
