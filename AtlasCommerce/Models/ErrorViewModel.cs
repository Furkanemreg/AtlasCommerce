using AtlasCommerce.Application.ViewModels;

namespace AtlasCommerce.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string? Message { get; set; }

        public WebsiteSettingsVM? Settings { get; set; }
    }
}
