using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AtlasCommerce.UI.Areas.Admin.Controllers
{
    public class ImageController : Controller
    {
        private readonly IImageService _imageService;

        public ImageController(IImageService imageService)
        {
            _imageService = imageService;
        }

        [HttpGet("GetImage/{id}")]
        public async Task<IActionResult> GetImage(Guid id)
        {
            var img = await _imageService.GetByOwnerAsync(id, nameof(Product));
            if (img == null)
                return NotFound();

            var mime = GetMimeType(img.FileName);
            return File(img.Data, mime);
        }

        private string GetMimeType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream",
            };
        }
    }
}
