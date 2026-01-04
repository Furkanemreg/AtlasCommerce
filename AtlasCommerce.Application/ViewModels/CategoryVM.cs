using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class CategoryVM
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Kategori adı alanı zorunludur.")]
        public required string Name { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public bool ShowInDropDown { get; set; } = false;

        [Display(Name = "Kategori Görseli")]
        public IFormFile? ImageFile { get; set; }

        public string? ImageDataUri { get; set; }

        public Guid? ImageId { get; set; }

        public Image? Image { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH.mm.ss}", ApplyFormatInEditMode = true)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH.mm.ss}", ApplyFormatInEditMode = true)]
        public DateTime? UpdatedAt { get; set; }

        public Guid? CreatedBy { get; set; }

        public Guid? UpdatedBy { get; set; }

        public AppUserVM? CreatedUser { get; set; }

        public AppUserVM? UpdatedUser { get; set; }

        public WebsiteSettingsVM? Settings { get; set; }
    }
}
