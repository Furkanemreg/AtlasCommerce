using AtlasCommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Domain.Common
{
    public class BaseEntity
    {
        public virtual Guid Id { get; set; }

        public virtual Guid? CreatedBy { get; set; }

        public virtual DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual Guid? UpdatedBy { get; set; }

        public virtual DateTime? UpdatedAt { get; set; }

        public virtual Guid? DeletedBy { get; set; }

        public virtual DateTime? DeletedAt { get; set; }

        public virtual bool IsDeleted { get; set; } = false;
    }
}
