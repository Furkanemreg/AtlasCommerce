using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class CategoryWithProductsVM
    {
        public Guid Id { get; set; }

        public required string Name { get; set; }

        public string? Description { get; set; }

        public string? ImageDataUri { get; set; }

        public List<SaleProductVM> Products { get; set; } = new();
    }
}
