using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AtlasCommerce.UI.Controllers
{
    public class SharedContoller : Controller
    {
        private readonly IBaseService<Product> _productService;
        private readonly IImageService _imageService;

        public SharedContoller(IBaseService<Product> productService, IImageService imageService)
        {
            _productService = productService;
            _imageService = imageService;
        }
    }
}
