using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class ResetPasswordVM
    {
        public Guid Id { get; init; }

        public string? UserName { get; init; }

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [Compare("Password", ErrorMessage = "Şifre alanları birbiri ile aynı olmak zorundadır")] 
        public string? ConfirmPassword { get; set; }

        public WebsiteSettingsVM? Settings { get; set; }
    }
}
