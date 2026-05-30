using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class OrderEditVM
    {
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        // Adres
        [Display(Name = "Ülke")]
        [StringLength(100)]
        public string? Country { get; set; }

        [Display(Name = "Şehir")]
        [StringLength(100)]
        public string? City { get; set; }

        [Display(Name = "İlçe")]
        [StringLength(100)]
        public string? District { get; set; }

        [Display(Name = "Posta Kodu")]
        [StringLength(20)]
        public string? ZipCode { get; set; }

        [Display(Name = "Adres")]
        [StringLength(1000)]
        public string? ShippingAddress { get; set; }

        // Sipariş durumu
        [Display(Name = "Sipariş Durumu")]
        public enmOrderStatus Status { get; set; }

        // Ürünler
        public List<OrderEditItemVM> Items { get; set; } = new();
    }

    public class OrderEditItemVM
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        [Display(Name = "Ürün")]
        public string? ProductName { get; set; }

        [Display(Name = "Barkod")]
        public string? Barcode { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Miktar")]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Birim Fiyat")]
        public decimal UnitPrice { get; set; }

        public decimal TotalPrice => Quantity * UnitPrice;
    }
}