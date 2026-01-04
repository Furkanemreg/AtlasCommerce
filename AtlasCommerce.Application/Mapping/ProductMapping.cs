using AutoMapper;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AtlasCommerce.Application.Mapping
{
    public class ProductMapping : Profile
    {
        public ProductMapping()
        {
            CreateMap<Product, ProductVM>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Barcode, opt => opt.MapFrom(src => src.Barcode))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.ShortDescription, opt => opt.MapFrom(src => src.ShortDescription))
                .ForMember(dest => dest.PurchasePriceExcludingTaxes, opt => opt.MapFrom(src => src.PurchasePriceExcludingTaxes))
                .ForMember(dest => dest.SalePriceExcludingTaxes, opt => opt.MapFrom(src => src.SalePriceExcludingTaxes))
                .ForMember(dest => dest.TaxRate, opt => opt.MapFrom(src => src.TaxRate))
                .ForMember(dest => dest.OTV, opt => opt.MapFrom(src => src.OTV))
                .ForMember(dest => dest.PurchasePriceIncludingTaxes, opt => opt.MapFrom(src => src.PurchasePriceIncludingTaxes))
                .ForMember(dest => dest.SalePriceIncludingTaxes, opt => opt.MapFrom(src => src.SalePriceIncludingTaxes))
                .ForMember(dest => dest.DiscountRate, opt => opt.MapFrom(src => src.DiscountRate))
                .ForMember(dest => dest.DiscountStartAt, opt => opt.MapFrom(src => src.DiscountStartAt))
                .ForMember(dest => dest.DiscountEndAt, opt => opt.MapFrom(src => src.DiscountEndAt))
                .ForMember(dest => dest.StockQuantity, opt => opt.MapFrom(src => src.StockQuantity))
                .ForMember(dest => dest.StockTracking, opt => opt.MapFrom(src => src.StockTracking))
                .ForMember(dest => dest.CriticalStockLevel, opt => opt.MapFrom(src => src.CriticalStockLevel))
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Weight, opt => opt.MapFrom(src => src.Weight))
                .ForMember(dest => dest.Lenght, opt => opt.MapFrom(src => src.Lenght))
                .ForMember(dest => dest.Width, opt => opt.MapFrom(src => src.Width))
                .ForMember(dest => dest.Height, opt => opt.MapFrom(src => src.Height))
                .ForMember(dest => dest.Attribute, opt => opt.MapFrom(src => src.Attribute))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                .ForMember(dest => dest.ShowInSelected, opt => opt.MapFrom(src => src.ShowInSelected))
                .ForMember(dest => dest.MainPhotoId, opt => opt.MapFrom(src => src.MainPhotoId))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images))
                .ForMember(dest => dest.UploadFiles, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
                .ReverseMap();

            // Entity -> ViewModel
            CreateMap<Product, SaleProductVM>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.SalePrice, opt => opt.MapFrom(src => src.SalePriceIncludingTaxes))
                .ForMember(dest => dest.MainImageId, opt => opt.MapFrom(src => (Guid?)src.MainPhotoId))
                .ForMember(dest => dest.SalePriceWithDiscount, opt => opt.Ignore()) // Hesaplanıyor zaten
                .ReverseMap()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.SalePriceIncludingTaxes, opt => opt.MapFrom(src => src.SalePrice))
                .ForMember(dest => dest.MainPhotoId, opt => opt.MapFrom(src => src.MainImageId ?? Guid.Empty))
                .ForMember(dest => dest.PurchasePriceExcludingTaxes, opt => opt.Ignore()) // ViewModel'de yok
                .ForMember(dest => dest.PurchasePriceIncludingTaxes, opt => opt.Ignore())
                .ForMember(dest => dest.SalePriceExcludingTaxes, opt => opt.Ignore())
                .ForMember(dest => dest.TaxRate, opt => opt.Ignore())
                .ForMember(dest => dest.OTV, opt => opt.Ignore())
                .ForMember(dest => dest.StockQuantity, opt => opt.Ignore())
                .ForMember(dest => dest.StockTracking, opt => opt.Ignore())
                .ForMember(dest => dest.CriticalStockLevel, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
                .ReverseMap();


            CreateMap<Image, ImageVM>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
                .ForMember(dest => dest.DataUri, opt => opt.MapFrom(src => $"data:{GetMime(src.FileName)};base64,{Convert.ToBase64String(src.Data)}"))
                .ReverseMap();
        }


        [NonAction]
        private string GetMime(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.TrimStart('.').ToLower();
            return ext switch
            {
                "png" => "image/png",
                "jpg" or "jpeg" => "image/jpeg",
                "gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }
    }
}
