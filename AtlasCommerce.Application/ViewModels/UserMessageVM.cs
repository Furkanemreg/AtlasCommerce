using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class UserMessageVM
    {
        [MaxLength(30, ErrorMessage = "İsim en fazla 30 karakterli olmalıdır.")]
        public string? Name { get; set; }

        [MaxLength(30, ErrorMessage = "Soyisim en fazla 30 karakterli olmalıdır.")]
        public string? Surname { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Lütfen geçerli bir E-Posta adresi giriniz.")]
        [MaxLength(50, ErrorMessage = "Lütfen geçerli uzunlukta bir E-Posta adresi giriniz.")]
        public required string EmailAddress { get; set; }

        [MaxLength(11, ErrorMessage = "Lütfen geçerli uzunlukta bir telefon numarası giriniz.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Başlık alanı boş bırakılamaz")]
        [MaxLength(100, ErrorMessage = "Başlık en fazla 100 karakterli olmalıdır.")]
        public required string Topic { get; set; }

        [Required(ErrorMessage = "Mesaj alanı boş bırakılamaz")]
        [MaxLength(500, ErrorMessage = "Mesajınız en fazla 500 karakterli olmalıdır.")]
        public required string Message { get; set; }

        public WebsiteSettingsVM? Settings { get; set; }
    }
}
