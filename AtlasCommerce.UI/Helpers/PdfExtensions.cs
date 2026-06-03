using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AtlasCommerce.UI.Helpers
{
    public static class PdfExtensions
    {
        public static void MetricCard(this IContainer container, string title, string value)
        {
            container
                .Border(1)
                .BorderColor("#E5E7EB")
                .Background("#FFFFFF")
                .Padding(12)
                .Column(col =>
                {
                    col.Item().Text(title)
                        .FontSize(10)
                        .FontColor("#6B7280");

                    col.Item().PaddingTop(5).Text(value)
                        .FontSize(16)
                        .Bold()
                        .FontColor("#111827");
                });
        }
    }
}