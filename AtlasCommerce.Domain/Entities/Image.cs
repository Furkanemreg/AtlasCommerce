using AtlasCommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Domain.Entities
{
    public class Image : BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        [Required]
        public long FileSize { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        // Sahip varlık
        public Guid? OwnerId { get; set; }

        public string? OwnerType { get; set; }
    }
}
