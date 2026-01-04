using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class LoginVM
    {
        [Required(ErrorMessage = "Kullanıcı adı gerekli.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Şifre gerekli.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; } = false;
        public string? ReturnUrl { get; set; }

        public WebsiteSettingsVM? Settings { get; set; }
    }
}
