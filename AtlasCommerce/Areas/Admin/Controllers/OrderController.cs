using AutoMapper;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AtlasCommerce.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]

    public class OrderController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IBaseService<WebsiteSettings> _settingsService;
        public OrderController(IMapper mapper, IBaseService<WebsiteSettings> settingsService)
        {
            _mapper = mapper;
            _settingsService = settingsService;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new EmptyVM();

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(vm);
        }
    }
}
