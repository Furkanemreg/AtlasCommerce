using AutoMapper;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;

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
    }
}
