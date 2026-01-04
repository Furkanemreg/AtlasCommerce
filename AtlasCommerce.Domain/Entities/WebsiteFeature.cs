using AtlasCommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Domain.Entities
{
    public class WebsiteFeature : BaseEntity
    {
        public string? IconHtml { get; set; }
        public string? Title { get; set; }
        public string? Text { get; set; }
    }
}
