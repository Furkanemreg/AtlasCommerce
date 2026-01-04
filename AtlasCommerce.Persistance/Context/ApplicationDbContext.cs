using AtlasCommerce.Domain.Common;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace AtlasCommerce.Persistance.Context
{
    public class ApplicationDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<UserMessage> UserMessages { get; set; } = null!;

        public DbSet<Category> Categories { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<Image> Images { get; set; }

        public DbSet<WebsiteSettings> WebsiteSettings { get; set; }
        public DbSet<WebsiteFeature> WebsiteFeature { get; set; }
        public DbSet<WebsiteService> WebsiteServices { get; set; }
        public DbSet<WebsiteBanner> WebsiteBanners { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Category -> Image (one-to-many olmadan, Many Images)
            builder.Entity<Category>()
                   .HasOne(c => c.Image)
                   .WithMany()
                   .HasForeignKey(c => c.ImageId)
                   .OnDelete(DeleteBehavior.SetNull);
        }

        public override int SaveChanges()
        {
            ApplyAuditInfo();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditInfo();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditInfo()
        {
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.Now;
                    entry.Entity.UpdatedAt = DateTime.Now;
                    entry.Entity.CreatedBy = Guid.TryParse(currentUserId, out var createdParsedId) ? createdParsedId : (Guid?)null;
                    entry.Entity.UpdatedBy = Guid.TryParse(currentUserId, out var updatedParsedId) ? updatedParsedId : (Guid?)null;

                    entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = true;
                    entry.Property(nameof(BaseEntity.UpdatedAt)).IsModified = true;
                    entry.Property(nameof(BaseEntity.CreatedBy)).IsModified = true;
                    entry.Property(nameof(BaseEntity.UpdatedBy)).IsModified = true;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = Guid.TryParse(currentUserId, out var parsedId) ? parsedId : (Guid?)null;

                    entry.Property(nameof(BaseEntity.UpdatedAt)).IsModified = true;
                    entry.Property(nameof(BaseEntity.UpdatedBy)).IsModified = true;
                }
            }

            foreach (var entry in ChangeTracker.Entries<AppUser>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.Now;
                    entry.Entity.UpdatedAt = DateTime.Now;
                    entry.Entity.CreatedBy = Guid.TryParse(currentUserId, out var createdParsedId) ? createdParsedId : (Guid?)null;
                    entry.Entity.UpdatedBy = Guid.TryParse(currentUserId, out var parsedId) ? parsedId : (Guid?)null;

                    entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = true;
                    entry.Property(nameof(BaseEntity.UpdatedAt)).IsModified = true;
                    entry.Property(nameof(BaseEntity.CreatedBy)).IsModified = true;
                    entry.Property(nameof(BaseEntity.UpdatedBy)).IsModified = true;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.Now;
                    entry.Entity.UpdatedBy = Guid.TryParse(currentUserId, out var parsedId) ? parsedId : (Guid?)null;

                    entry.Property(nameof(BaseEntity.UpdatedAt)).IsModified = true;
                    entry.Property(nameof(BaseEntity.UpdatedBy)).IsModified = true;
                }
            }
        }
    }
}
