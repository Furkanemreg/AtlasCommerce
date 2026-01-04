using AtlasCommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Domain.Entities
{
    public class UserMessage : BaseEntity
    {
        public Guid? UserId { get; set; }

        [MaxLength(30, ErrorMessage = "İsim en fazla 30 karakterli olmalıdır.")]
        public string? Name { get; set; }

        [MaxLength(30, ErrorMessage = "Soyisim en fazla 30 karakterli olmalıdır.")]
        public string? Surname { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Lütfen geçerli bir E-Posta adresi giriniz.")]
        [MaxLength(60, ErrorMessage = "Lütfen geçerli uzunlukta bir E-Posta adresi giriniz.")]
        public required string EmailAddress { get; set; }

        [MaxLength(20, ErrorMessage = "Lütfen geçerli uzunlukta bir telefon numarası giriniz.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Başlık alanı boş bırakılamaz")]
        [MaxLength(100, ErrorMessage = "Başlık en fazla 20 karakterli olmalıdır.")]
        public required string Topic { get; set; }

        [NotMapped]
        public string ShortTopic =>
            !string.IsNullOrEmpty(Topic) && Topic.Length > 20
            ? Topic.Substring(0, 20) + "..."
            : Topic;


        [Required(ErrorMessage = "Mesaj alanı boş bırakılamaz")]
        [MaxLength(500, ErrorMessage = "Mesajınız en fazla 200 karakterli olmalıdır.")]
        public required string Message { get; set; }

        public bool IsRead { get; set; } = false;
    }
}
