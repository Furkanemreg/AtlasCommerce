namespace AtlasCommerce.Application.ViewModels
{
    public class CategoryPageVM
    {
        public required CategoryVM Category { get; set; }
        public List<SaleProductVM> Products { get; set; } = new();
        public ProductFilterVM Filter { get; set; }

        // Pagination
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        
        // Information
        public int TotalRecords { get; set; }
        public int ProductsCount => Products?.Count ?? 0;

        public WebsiteSettingsVM? Settings { get; set; }
    }
}
