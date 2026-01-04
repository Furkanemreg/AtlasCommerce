using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class ProductVM
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Ürün adı alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "Ürün adı en fazla 100 karakter olabilir.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Ürün kodu alanı zorunludur.")]
        [StringLength(50, ErrorMessage = "Ürün kodu en fazla 50 karakter olabilir.")]
        public string Barcode { get; set; }

        [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
        public string? Description { get; set; }

        [StringLength(120, ErrorMessage = "Kısa açıklama en fazla 120 karakter olabilir.")]
        public string? ShortDescription { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Alış fiyatı sıfırdan küçük olamaz.")]
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        public decimal PurchasePriceExcludingTaxes { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Satış fiyatı sıfırdan küçük olamaz.")]
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        public decimal SalePriceExcludingTaxes { get; set; }

        [Range(0, 100, ErrorMessage = "Vergi oranı 0 ile 100 arasında olmalıdır.")]
        [DisplayFormat(DataFormatString = "{0}", ApplyFormatInEditMode = true)]
        public int TaxRate { get; set; } = 0;

        [Range(0, 100, ErrorMessage = "OTV oranı 0 ile 100 arasında olmalıdır.")]
        [DisplayFormat(DataFormatString = "{0}", ApplyFormatInEditMode = true)]
        public int OTV { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Alış fiyatı sıfırdan küçük olamaz.")]
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        public decimal PurchasePriceIncludingTaxes { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Satış fiyatı sıfırdan küçük olamaz.")]
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        public decimal SalePriceIncludingTaxes { get; set; }

        private bool? _isDiscountActiveOverride;

        public bool IsDiscountActive
        {
            get
            {
                // Eğer dışarıdan bir değer atanmışsa onu döndür,
                // aksi takdirde dinamik olarak hesapla:
                return _isDiscountActiveOverride
                       ?? (DiscountRate > 0
                           && DiscountStartAt <= DateTime.Now
                           && DiscountEndAt >= DateTime.Now);
            }
            set
            {
                // null atarsanız (örn. = null), otomatiğe geri dönersiniz:
                _isDiscountActiveOverride = value;
            }
        }

        [Range(0, 100, ErrorMessage = "İndirim oranı 0 ile 100 arasında olmalıdır.")]
        public int DiscountRate { get; set; } = 0;

        public DateTime? DiscountStartAt { get; set; } = DateTime.Now;

        public DateTime? DiscountEndAt { get; set; } = DateTime.Now + TimeSpan.FromDays(7);

        [NotMapped]
        public decimal SalePrice
        {
            get
            {
                // 1) Vergiler Hariç Satış fiyatı
                var excl = SalePriceExcludingTaxes;
                // 2) ÖTV+KDV ekle
                var incl = excl * (1 + (OTV + TaxRate) / 100m);
                // 3) İndirim uygulanıyorsa düş
                if (DiscountRate > 0
                    && DiscountStartAt <= DateTime.Now
                    && DiscountEndAt >= DateTime.Now)
                {
                    incl = incl * (1 - DiscountRate / 100m);
                }
                return decimal.Round(incl, 2);
            }
        }

        [Range(0, double.MaxValue, ErrorMessage = "Stok miktarı negatif olamaz.")]
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        public decimal StockQuantity { get; set; } = 0;

        public bool StockTracking { get; set; } = false;

        [Range(0, double.MaxValue, ErrorMessage = "Kritik stok seviyesi negatif olamaz.")]
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        public decimal? CriticalStockLevel { get; set; } = 0;

        [Required(ErrorMessage = "Lütfen bir kategori seçin.")]
        public Guid CategoryId { get; set; }

        public CategoryVM? Category { get; set; }

        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();

        [Required(ErrorMessage = "Lütfen bir renk seçin.")]
        public int? Color { get; set; }

        public IEnumerable<SelectListItem>? Colors { get; set; }

        public bool ShowInSelected { get; set; } = false;

        [Range(0, double.MaxValue, ErrorMessage = "Ağırlık negatif olamaz.")]
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        public decimal Weight { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Uzunluk negatif olamaz.")]
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        public decimal Lenght { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Genişlik negatif olamaz.")]
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        public decimal Width { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Yükseklik negatif olamaz.")]
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        public decimal Height { get; set; } = 0;

        public string? Attribute { get; set; }

        public Guid? MainPhotoId { get; set; }

        [Required(ErrorMessage = "Lütfen bir ana fotoğraf seçin.")]
        public int? MainPhotoIndex { get; set; }

        public List<Guid> ExistingImageIds { get; set; } = new();

        public string? ImageDataUri { get; set; }

        public List<ImageVM> Images { get; set; } = new();

        public List<IFormFile>? UploadFiles { get; set; }

        public bool IsActive { get; set; } = true;

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
