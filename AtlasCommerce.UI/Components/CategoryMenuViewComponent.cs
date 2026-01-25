using Microsoft.AspNetCore.Mvc;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;

namespace AtlasCommerce.UI.Components
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly IBaseService<Category> _categoryService;
        private readonly IImageService _imageService;

        public CategoryMenuViewComponent(IBaseService<Category> categoryService, IImageService imageService)
        {
            _categoryService = categoryService;
            _imageService = imageService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var dropdownCategories = await _categoryService.GetAllAsync(i => i.ShowInDropDown && i.IsActive);

            var list = new List<DropdownCategoryVM>();
            foreach (var c in dropdownCategories.Where(c => !string.IsNullOrEmpty(c.Name)))
            {
                string? imageDataUri = null;
                var imageResult = await _imageService.GetByOwnerAsync(c.Id, nameof(Category));
                if (imageResult != null && imageResult.Data != null)
                    imageDataUri = $"data:image/png;base64,{Convert.ToBase64String(imageResult.Data)}";

                list.Add(new DropdownCategoryVM
                {
                    Id = c.Id,
                    Name = c.Name!,
                    ImageDataUri = imageDataUri
                });
            }

            return View(list);
        }
    }
}
