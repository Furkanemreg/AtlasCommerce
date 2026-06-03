using AutoMapper;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using AtlasCommerce.UI.Helpers;

namespace AtlasCommerce.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
		private readonly ILogger<DashboardController> _logger;
        private readonly IBaseService<UserMessage> _userMessageService;
        private readonly IDashboardService _dashboardService;
        private readonly IMapper _mapper;
        private readonly IBaseService<WebsiteSettings> _settingsService;

        public DashboardController(IBaseService<UserMessage> userMessageService, IBaseService<WebsiteSettings> settingsService, ILogger<DashboardController> logger, IMapper mapper, IDashboardService dashboardService)
        {
            _userMessageService = userMessageService;
            _logger = logger;
            _mapper = mapper;
            _dashboardService = dashboardService;
            _settingsService = settingsService;
        }

        public async Task<IActionResult> Index()
        {
            DashboardVM vm = _dashboardService.GetDashboard();

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(vm);
        }

        public IActionResult DownloadDashboardPdf()
        {
            var vm = _dashboardService.GetDashboard();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // HEADER
                    page.Header()
                        .Background("#111827")
                        .Padding(15)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("ATLAS COMMERCE")
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor("#FFFFFF");

                                col.Item().Text("Dashboard Report")
                                    .FontSize(11)
                                    .FontColor("#9CA3AF");
                            });

                            row.ConstantItem(120)
                                .AlignRight()
                                .AlignMiddle()
                                .Column(col =>
                                {
                                    col.Item().Text(DateTime.Now.ToString("dd.MM.yyyy"))
                                        .FontColor("#E5E7EB");

                                    col.Item().Text(DateTime.Now.ToString("HH:mm"))
                                        .FontColor("#9CA3AF")
                                        .FontSize(9);
                                });
                        });

                    // CONTENT
                    page.Content()
                        .PaddingTop(15)
                        .Column(col =>
                        {
                            // METRICS
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().MetricCard("Orders", vm.TotalOrderCount.ToString("n2"));
                                row.ConstantItem(10);

                                row.RelativeItem().MetricCard("Products", vm.TotalProductCount.ToString("n2"));
                                row.ConstantItem(10);

                                row.RelativeItem().MetricCard("Sales", $"{vm.TotalSales.ToString("n2")} ₺");
                            });

                            col.Item().PaddingTop(20);

                            // RECENT ORDERS
                            col.Item().Column(section =>
                            {
                                section.Item().Text("Recent Orders")
                                    .FontSize(14)
                                    .Bold()
                                    .FontColor("#111827");

                                section.Item().PaddingTop(8).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Background("#F3F4F6").Padding(6).Text("Customer").Bold();
                                        header.Cell().Background("#F3F4F6").Padding(6).Text("Date").Bold();
                                        header.Cell().Background("#F3F4F6").Padding(6).Text("Status").Bold();
                                    });

                                    foreach (var order in vm.RecentOrders ?? new())
                                    {
                                        table.Cell().Padding(6).Text(order.FullName);
                                        table.Cell().Padding(6).Text(order.CreatedAt.ToString("dd.MM.yyyy"));
                                        table.Cell().Padding(6).Text(order.Status);
                                    }
                                });
                            });

                            col.Item().PaddingTop(20);

                            // MESSAGES
                            col.Item().Column(section =>
                            {
                                section.Item().Text("Messages")
                                    .FontSize(14)
                                    .Bold()
                                    .FontColor("#111827");

                                section.Item().PaddingTop(8).Column(msgCol =>
                                {
                                    foreach (var msg in vm.DashboardMessages ?? new())
                                    {
                                        msgCol.Item()
                                            .Background("#F9FAFB")
                                            .BorderLeft(3)
                                            .BorderColor("#3B82F6")
                                            .Padding(8)
                                            .Text($"{msg.FirstName} {msg.LastName} - {msg.Topic}");
                                    }
                                });
                            });
                        });

                    // FOOTER
                    page.Footer()
                        .AlignCenter()
                        .PaddingTop(10)
                        .Text(x =>
                        {
                            x.Span("Atlas Commerce © ");
                            x.Span(DateTime.Now.Year.ToString());
                        });
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", "dashboard-report.pdf");
        }

    }
}
