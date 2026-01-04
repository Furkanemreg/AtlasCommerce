using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class DropdownCategoryVM
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? ImageDataUri { get; set; }
    }
}
