using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class FeatureVM
    {
        public Guid Id { get; set; }
        public string? IconHtml { get; set; }
        public string? Title { get; set; }
        public string? Text { get; set; }
    }
}
