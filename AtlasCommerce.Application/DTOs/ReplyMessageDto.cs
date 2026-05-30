using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.DTOs
{
    public class ReplyMessageDto
    {
        public Guid MessageId { get; set; }
        public string Reply { get; set; }
    }
}
