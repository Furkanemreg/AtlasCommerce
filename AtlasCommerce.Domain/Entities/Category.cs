using AtlasCommerce.Domain.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Domain.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public bool ShowInDropDown { get; set; }

        public Guid? ImageId { get; set; }

        public Image? Image { get; set; }
    }
}
