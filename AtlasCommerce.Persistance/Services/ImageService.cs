using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Domain.Entities;
using AtlasCommerce.Persistance.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Persistance.Services
{
    public class ImageService : IImageService
    {
        public ApplicationDbContext _context { get; set; }

        public ImageService(ApplicationDbContext context) : base()
        {
            _context = context;
        }

        public async Task<Image?> GetByOwnerAsync(Guid ownerId, string ownerType)
        {
            return await _context.Set<Image>().Where(i => i.OwnerId == ownerId && i.OwnerType == ownerType && i.IsDeleted == false).FirstOrDefaultAsync();
        }

        public async Task<ICollection<Image>> GetAllByOwnerAsync(Guid ownerId, string ownerType)
        {
            return await _context.Set<Image>().Where(i => i.OwnerId == ownerId && i.OwnerType == ownerType && !i.IsDeleted).ToListAsync();
        }
    }
}
