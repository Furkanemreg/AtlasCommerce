using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class ImageVM
    {
        public Guid Id { get; set; }

        public string FileName { get; set; } = null!;

        public string DataUri { get; set; } = null!;

        public bool IsMain { get; set; }
    }
}
