using AutoMapper;
using Azure.Core;
using AtlasCommerce.Application.DTOs;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Configuration;
using NuGet.ProjectModel;
using AtlasCommerce.Application.Interfaces.Caching;

namespace AtlasCommerce.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SettingController : Controller
    {
        private readonly IBaseService<WebsiteSettings> _settingsService;
        private readonly IBaseService<WebsiteFeature> _featureService;
        private readonly IBaseService<WebsiteService> _serviceService;
        private readonly IBaseService<WebsiteBanner> _bannerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<SettingController> _logger;

        private readonly ICacheService _cacheService; // generic
        private readonly ISettingsCacheService _settingsCacheService;
        private readonly IBannerCacheService _bannerCacheService;
        private readonly IDropdownCacheService _dropdownCacheService;

        public SettingController(
            IBaseService<WebsiteSettings> settingsService,
            IBaseService<WebsiteFeature> featureService,
            IBaseService<WebsiteService> serviceService,
            IBaseService<WebsiteBanner> bannerService,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<SettingController> logger,
            ICacheService cacheService,
            ISettingsCacheService settingsCacheService,
            IBannerCacheService bannerCacheService,
            IDropdownCacheService dropdownCacheService)
        {
            _settingsService = settingsService;
            _featureService = featureService;
            _serviceService = serviceService;
            _bannerService = bannerService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
            _settingsCacheService = settingsCacheService;
            _bannerCacheService = bannerCacheService;
            _dropdownCacheService = dropdownCacheService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Tek kayıt olacak mantığıyla
            var entity = (await _settingsService.GetAllAsync()).FirstOrDefault();

            if (entity == null)
                entity = new WebsiteSettings();

            var vm = _mapper.Map<WebsiteSettingsVM>(entity);

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Save(WebsiteSettingsVM model, IFormFile? LogoFile)
        {
            if (!ModelState.IsValid)
                return View("Index", model);

            try
            {
                var entity = await _settingsService.GetByIdAsync(model.Id);

                if (LogoFile != null && LogoFile.Length > 0)
                {
                    // Basit upload örneği
                    var fileName = Guid.NewGuid() + Path.GetExtension(LogoFile.FileName);
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await LogoFile.CopyToAsync(stream);
                    }

                    model.LogoPath = "/uploads/" + fileName;
                }
                else
                {
                    // LogoFile seçilmediyse, mevcut entity.LogoPath'i koru
                    model.LogoPath = entity?.LogoPath;
                }

                if (entity == null)
                {
                    entity = _mapper.Map<WebsiteSettings>(model);
                    await _settingsService.AddAsync(entity);
                }
                else
                {
                    _mapper.Map(model, entity);
                    await _settingsService.UpdateAsync(entity);
                }

                await _unitOfWork.Commit();

                // CACHE INVALIDATE
                await _settingsCacheService.RemoveAsync();

                TempData["SuccessMessage"] = "Settings have been saved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving settings.");
                TempData["ErrorMessage"] = "An error occurred.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddFeature(FeatureVM model)
        {
            if (!ModelState.IsValid)
                return BadRequest("An error occurred. Please enter valid information.");

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();

            var feature = new WebsiteFeature
            {
                IconHtml = model.IconHtml,
                Title = model.Title,
                Text = model.Text,
            };
            
            await _featureService.AddAsync(feature);
            await _unitOfWork.Commit();

            // homepage / dropdown etkilenir
            await _dropdownCacheService.RemoveAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFeature([FromBody] DeleteRequestFromBodyDTO request)
        {
            var feature = await _featureService.GetByIdAsync(request.Id);
            if (feature == null)
            {
                return Json(new { success = false, message = "Record not found." });
            }

            await _featureService.DeleteAsync(feature);
            await _unitOfWork.Commit();

            // homepage / dropdown etkilenir
            await _dropdownCacheService.RemoveAsync();

            return Json(new { success = true, message = "Record deleted successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> AddService(IFormFile ImageFile, string Title, string Text)
        {
            var service = new WebsiteService
            {
                Title = Title,
                Text = Text
            };

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploadRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/services");

                if (!Directory.Exists(uploadRoot))
                    Directory.CreateDirectory(uploadRoot);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ImageFile.FileName)}";
                var filePath = Path.Combine(uploadRoot, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                service.ImagePath = $"/uploads/services/{fileName}";
            }

            await _serviceService.AddAsync(service);
            await _unitOfWork.Commit();

            await _cacheService.RemoveAsync("ui:services");

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteService([FromBody] DeleteRequestFromBodyDTO request)
        {
            var service = await _serviceService.GetByIdAsync(request.Id);
            if (service == null)
                return Json(new { success = false, message = "Record not found." });

            await _serviceService.DeleteAsync(service);
            await _unitOfWork.Commit();

            await _cacheService.RemoveAsync("ui:services");

            return Json(new { success = true, message = "Service deleted successfully." });
        }


        [HttpPost]
        public async Task<IActionResult> AddBanner(IFormFile ImageFile, string Title, string Text)
        {
            var banner = new WebsiteBanner
            {
                Title = Title,
                Text = Text
            };

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploadRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/banners");

                if (!Directory.Exists(uploadRoot))
                    Directory.CreateDirectory(uploadRoot);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ImageFile.FileName)}";
                var filePath = Path.Combine(uploadRoot, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                banner.ImagePath = $"/uploads/banners/{fileName}";
            }

            await _bannerService.AddAsync(banner);
            await _unitOfWork.Commit();

            // CACHE INVALIDATE
            await _bannerCacheService.RemoveAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBanner([FromBody] DeleteRequestFromBodyDTO request)
        {
            var banner = await _bannerService.GetByIdAsync(request.Id);
            if (banner == null)
                return Json(new { success = false, message = "Record not found." });

            await _bannerService.DeleteAsync(banner);
            await _unitOfWork.Commit();

            // CACHE INVALIDATE
            await _bannerCacheService.RemoveAsync();

            return Json(new { success = true, message = "Banner deleted successfully." });
        }
    }
}
