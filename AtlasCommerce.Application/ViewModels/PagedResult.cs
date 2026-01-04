namespace AtlasCommerce.Application.ViewModels
{
    public class PagedResult<T>
    {
        public List<T?> Items { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public string? SearchTerm { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public WebsiteSettingsVM? Settings { get; set; }
    }
}
