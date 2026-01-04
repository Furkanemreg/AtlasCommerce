using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class RegisterVM
    {
        [Required(ErrorMessage = "İsim gerekli.")]
        [MinLength(2, ErrorMessage = "İsim en az 2 karakter olmalıdır.")]
        [MaxLength(20, ErrorMessage = "İsim en fazla 20 karakter olmalıdır.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Soyisim gerekli.")]
        [MinLength(2, ErrorMessage = "Soyisim en az 2 karakter olmalıdır.")]
        [MaxLength(20, ErrorMessage = "Soyisim en fazla 20 karakter olmalıdır.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı gerekli.")]
        [MinLength(2, ErrorMessage = "Kullanıcı adı en az 2 karakter olmalıdır.")]
        [MaxLength(20, ErrorMessage = "Kullanıcı adı en fazla 20 karakter olmalıdır.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "E-Posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Lütfen geçerli bir E-Posta adresi giriniz.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "Telefon numarası + ile başlayabilir ve 10-15 rakam içermelidir.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Şifre gerekli.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Şifre tekrar gerekli.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; }

        public WebsiteSettingsVM? Settings { get; set; }
    }
}
