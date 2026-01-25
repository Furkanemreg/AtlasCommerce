using AutoMapper;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AtlasCommerce.UI.Components
{
    public class WebsiteSettingsViewComponent : ViewComponent
    {
        private readonly IBaseService<WebsiteSettings> _settingsService;
        private readonly IMapper _mapper;

        public WebsiteSettingsViewComponent(
            IBaseService<WebsiteSettings> settingsService,
            IMapper mapper)
        {
            _settingsService = settingsService;
            _mapper = mapper;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var entity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            var vm = _mapper.Map<WebsiteSettingsVM>(entity ?? new WebsiteSettings());
            return View(vm);
        }
    }
}
