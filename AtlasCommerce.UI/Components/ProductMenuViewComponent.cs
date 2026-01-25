using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AtlasCommerce.UI.Components
{
    public class ProductMenuViewComponent : ViewComponent
    {
        private readonly IBaseService<Product> _productService;
        private readonly IBaseService<Category> _categoryService;
        private readonly IImageService _imageService;

        public ProductMenuViewComponent(IBaseService<Product> productService, IBaseService<Category> categoryService, IImageService imageService)
        {
            _categoryService = categoryService;
            _productService = productService;
            _imageService = imageService;
        }

        //public async Task<IViewComponentResult> InvokeAsync(Guid? categoryId = null)
        //{
        //    var products = categoryId.HasValue
        //        ? await _productService.GetAllAsync(p => p.CategoryId == categoryId.Value && p.IsActive)
        //        : await _productService.GetAllAsync(p => p.IsActive);

        //    var vms = new List<SaleProductVM>();
        //    foreach (var p in products.Take(8))
        //    {
        //        string? dataUri = null;
        //        var img = await _imageService.GetByOwnerAsync(p.Id, nameof(Product));
        //        if (img?.Data != null)
        //            dataUri = $"data:image/png;base64,{Convert.ToBase64String(img.Data)}";

        //        vms.Add(new SaleProductVM
        //        {
        //            Id = p.Id,
        //            Title = p.Name!,
        //            Barcode = p.Barcode,
        //            Description = p.Description,
        //            ShortDescription = p.ShortDescription,
        //            SalePriceWithoutDiscount = p.SalePriceIncludingTaxes,
        //            SalePriceWithDiscount = p.SalePriceIncludingTaxes,
        //            DiscountRate = p.DiscountRate,
        //            DiscountStartAt = p.DiscountStartAt,
        //            DiscountEndAt = p.DiscountEndAt,
        //            CategoryId = p.CategoryId,
        //            Weight = p.Weight,
        //            Height = p.Height,
        //            Width = p.Width,
        //            Lenght = p.Lenght,
        //            Attribute = p.Attribute,
        //            MainImageId = p.MainPhotoId,
        //            Images = new List<ImageVM>
        //            {
        //                new ImageVM
        //                {
        //                    Id = Guid.NewGuid(),
        //                    FileName = p.Name ?? "",
        //                    DataUri = dataUri ?? "",
        //                    IsMain = true
        //                }
        //            }
        //        });
        //    }

        //    return View(vms);
        //}


        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _categoryService.GetAllAsync(c => c.IsActive && c.ShowInDropDown);
            var model = new List<CategoryWithProductsVM>();

            foreach (var cat in categories)
            {
                var products = await _productService.GetAllAsync(p => p.CategoryId == cat.Id && p.IsActive);
                var productVMs = new List<SaleProductVM>();

                foreach (var p in products.Take(8)) // her kategori için ilk 8 ürün
                {
                    string? imageDataUri = null;
                    var img = await _imageService.GetByOwnerAsync(p.Id, nameof(Product));
                    if (img?.Data != null)
                        imageDataUri = $"data:image/png;base64,{Convert.ToBase64String(img.Data)}";

                    productVMs.Add(new SaleProductVM
                    {
                        Id = p.Id,
                        Title = p.Name!,
                        Barcode = p.Barcode!
                    });
                }

                model.Add(new CategoryWithProductsVM
                {
                    Id = cat.Id,
                    Name = cat.Name!,
                    Products = productVMs
                });
            }

            return View(model);
        }
    }

}
