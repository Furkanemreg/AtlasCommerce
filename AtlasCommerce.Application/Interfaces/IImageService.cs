using AtlasCommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Interfaces
{
    public interface IImageService
    {
        Task<Image?> GetByOwnerAsync(Guid ownerId, string ownerType);

        Task<ICollection<Image>> GetAllByOwnerAsync(Guid ownerId, string ownerType);
    }
}
