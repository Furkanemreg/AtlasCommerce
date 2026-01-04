using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class BannerVM
    {
        public Guid Id { get; set; }
        public string? ImagePath { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
    }
}
