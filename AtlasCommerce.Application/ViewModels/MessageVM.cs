using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class MessageVM
    {
        public Guid? UserId { get; set; }
        public Guid Id { get; set; }

        public required string FirstName { get; set; }

        public required string LastName { get; set; }

        public required string EmailAddress { get; set; }

        public required string PhoneNumber { get; set; }

        public required string Topic { get; set; }

        [NotMapped]
        public string ShortTopic => !string.IsNullOrEmpty(Topic) && Topic.Length > 20 ? Topic.Substring(0, 20) + "..." : Topic;

        public required string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; }

        // ADMIN REPLY
        public string? AdminReply { get; set; }
        public DateTime? RepliedAt { get; set; }
        public Guid? RepliedBy { get; set; }

        public WebsiteSettingsVM? Settings { get; set; }
    }
}
