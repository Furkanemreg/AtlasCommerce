using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class CheckoutVM
    {
        // Address snapshot
        [Required(ErrorMessage = "Lütfen adres bilgisini giriniz")]
        [MaxLength(300)]
        public string ShippingAddress { get; set; } = null!;

        [MaxLength(20)]
        public string? ZipCode { get; set; } = null!;

        [Required(ErrorMessage = "Lütfen ilçe bilgisini giriniz")]
        [MaxLength(50)]
        public string District { get; set; } = null!;

        [Required(ErrorMessage = "Lütfen şehir bilgisini giriniz")]
        [MaxLength(50)]
        public string City { get; set; } = null!;

        [Required(ErrorMessage = "Lütfen ülke bilgisini giriniz")]
        [MaxLength(50)]
        public string Country { get; set; } = null!; 
        
        // Taslak siparişi ödemek için
        public Guid? OrderId { get; set; }
        public Guid? PaymentId { get; set; }
        public string? OrderNumber { get; set; }

        // UI
        public List<CartItemVM> Items { get; set; } = new();
        public decimal VatTotal { get; set; } = 0;
        public decimal OtvTotal { get; set; } = 0;
        public decimal TaxTotal => VatTotal + OtvTotal;
        public decimal SubTotal { get; set; }
        public decimal Total { get; set; }
        public bool DeliverPhysical { get; set; } = false;
        public WebsiteSettingsVM? Settings { get; set; }
    }
}
