using AtlasCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class AppUserVM
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı alanı zorunludur.")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Soyadı alanı zorunludur.")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "E-posta adresi alanı zorunludur.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Telefon Numarası alanı zorunludur.")]
        public string? PhoneNumber { get; set; }

        public List<string> Roles { get; set; } = new List<string>();

        public string? UserRole { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH.mm.ss}", ApplyFormatInEditMode = true)]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH.mm.ss}", ApplyFormatInEditMode = true)]
        public DateTime? UpdatedAt { get; set; }

        public Guid? CreatedBy { get; set; }

        public Guid? UpdatedBy { get; set; }

        public AppUserVM? CreatedUser { get; set; }

        public AppUserVM? UpdatedUser { get; set; }

        public bool IsActive { get; set; } = true;

        [NotMapped]
        public string FullName => string.Join(" ", FirstName, LastName);
        public WebsiteSettingsVM? Settings { get; set; }
    }
}
