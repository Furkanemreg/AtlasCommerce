using AutoMapper;
using AtlasCommerce.Application.Extensions;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Application.Wrappers;
using AtlasCommerce.Domain.Entities;
using AtlasCommerce.Persistance.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Eventing.Reader;
using System.Linq.Expressions;
using AtlasCommerce.Application.Interfaces.Caching;
using AtlasCommerce.Persistance.Services.Caching;

namespace AtlasCommerce.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : BaseController<Category>
    {
        private readonly IMapper _mapper;
        private readonly IAccountService _accountService;
        private readonly IImageService _imageService;
        private readonly IBaseService<Image> _imageBaseService;
        private readonly IBaseService<WebsiteSettings> _settingsService;
        private readonly ICategoryCacheService _categoryCacheService;
        private readonly IHomeCacheService _homeCacheService;
        private readonly ICategoryPageCacheService _categoryPageCacheService;

        public CategoryController(IBaseService<Category> baseService, 
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IAccountService accountService, 
            IBaseService<Image> imageBaseService, 
            IBaseService<WebsiteSettings> settingsService, 
            IImageService imageService, 
            ICategoryCacheService categoryCacheService,
            IHomeCacheService homeCacheService,
            ICategoryPageCacheService categoryPageCacheService) : base(baseService, unitOfWork)
        {
            _mapper = mapper;
            _accountService = accountService;
            _imageBaseService = imageBaseService;
            _imageService = imageService;
            _settingsService = settingsService;
            _categoryCacheService = categoryCacheService;
            _homeCacheService = homeCacheService;
            _categoryPageCacheService = categoryPageCacheService;
        }

        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
        {
            Expression<Func<Category, bool>> predicate = x => true;
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.Trim().ToLower();
                predicate = predicate.And(x =>
                    (!string.IsNullOrEmpty(x.Name) && x.Name.ToLower().Contains(term)) ||
                    (!string.IsNullOrEmpty(x.Description) && x.Description.ToLower().Contains(term))
                );
            }

            var (entities, totalCount) = await _baseService.GetPagedAsync(pageNumber, pageSize, predicate);
            var vmList = _mapper.Map<List<CategoryVM>>(entities);

            foreach (var vm in vmList)
            {
                if (vm.CreatedBy != Guid.Empty)
                    vm.CreatedUser = await _accountService.GetByIdAsync(vm.CreatedBy.ToString());

                if (vm.UpdatedBy != Guid.Empty)
                    vm.UpdatedUser = await _accountService.GetByIdAsync(vm.UpdatedBy.ToString());

                if (vm.ImageId != Guid.Empty)
                {
                    var image = await _imageService.GetByOwnerAsync(vm.Id, nameof(Category));
                    if (image != null)
                    {
                        var ext = Path.GetExtension(image.FileName)?.TrimStart('.').ToLower();
                        var mime = ext switch
                        {
                            "png" => "image/png",
                            "jpg" or "jpeg" => "image/jpeg",
                            "gif" => "image/gif",
                            _ => "application/octet-stream"
                        };

                        vm.ImageDataUri = $"data:{mime};base64,{Convert.ToBase64String(image.Data)}";
                        vm.ImageId = image.Id;
                    }
                }
            }

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            var settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            var model = new PagedResult<CategoryVM>
            {
                Items = vmList,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Settings = settings
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id = null)
        {
            CategoryVM vm;
            if (string.IsNullOrEmpty(id) || Guid.Parse(id) == Guid.Empty)
            {
                vm = new CategoryVM
                {
                    Name = string.Empty,
                    CreatedUser = await _accountService.GetByUserName(User.Identity?.Name)
                };
            }
            else
            {
                var guid = Guid.Parse(id);
                var category = await _baseService.GetByIdAsync(guid);
                vm = _mapper.Map<CategoryVM>(category);

                vm.CreatedUser = await _accountService.GetByIdAsync(vm.CreatedBy.ToString());
                if (vm.UpdatedBy != null && vm.UpdatedBy != Guid.Empty)
                    vm.UpdatedUser = await _accountService.GetByIdAsync(vm.UpdatedBy.ToString());

                var image = await _imageService.GetByOwnerAsync(guid, nameof(Category));
                if (image != null)
                {
                    string mimeType = "image/png"; // veya uzantıya göre image/jpeg
                    string base64 = Convert.ToBase64String(image.Data);
                    vm.ImageDataUri = $"data:{mimeType};base64,{base64}";
                }
            }


            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            var settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);
            vm.Settings = settings;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] CategoryVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var isNew = vm.Id == Guid.Empty;
            Category category;
            bool oldShowInDropDown = false;

            if (isNew)
            {
                category = _mapper.Map<Category>(vm);
                await _baseService.AddAsync(category);
                await _unitOfWork.Commit();
            }
            else
            {
                category = await _baseService.GetByIdAsync(vm.Id);
                if (category == null) return NotFound();

                oldShowInDropDown = category.ShowInDropDown;
                _mapper.Map(vm, category);

                await _baseService.UpdateAsync(category);
                await _unitOfWork.Commit();
            }

            if (category.ShowInDropDown && !oldShowInDropDown)
            {
                var dropdownCount = (await _baseService.GetAllAsync(i => i.ShowInDropDown && i.IsActive)).Count;
                if (dropdownCount >= 8)
                    return Json(new { success = false, message = "A maximum of 8 categories can be displayed in the main menu." });
            }

            if (!category.IsActive)
                category.ShowInDropDown = false;

            if (vm.ImageFile != null)
            {
                byte[] data;
                using var ms = new MemoryStream();
                await vm.ImageFile.OpenReadStream().CopyToAsync(ms);
                data = ms.ToArray();

                using var img = System.Drawing.Image.FromStream(new MemoryStream(data));

                // Daha önce eklenmiş bir resim var mı?
                var existingImage = await _imageService.GetByOwnerAsync(category.Id, nameof(Category));

                if (existingImage != null)
                {
                    // Güncelle
                    existingImage.FileName = vm.ImageFile.FileName;
                    existingImage.Data = data;
                    existingImage.FileSize = vm.ImageFile.Length;
                    existingImage.Width = img.Width;
                    existingImage.Height = img.Height;
                    await _imageBaseService.UpdateAsync(existingImage);
                    category.ImageId = existingImage.Id;
                }
                else
                {
                    // Yeni resim ekle
                    var newImage = new Image
                    {
                        FileName = vm.ImageFile.FileName,
                        Data = data,
                        FileSize = vm.ImageFile.Length,
                        Width = img.Width,
                        Height = img.Height,
                        OwnerId = category.Id,
                        OwnerType = nameof(Category)
                    };
                    await _imageBaseService.AddAsync(newImage);
                    category.ImageId = newImage.Id;
                }

                await _baseService.UpdateAsync(category);
                await _unitOfWork.Commit();
            }
            else
            {
                category.ImageId = vm.ImageId;
                await _baseService.UpdateAsync(category);
                await _unitOfWork.Commit();
            }

            await _categoryCacheService.RemoveAsync();
            await _homeCacheService.RemoveAsync();
            await _categoryPageCacheService.RemoveAllAsync();

            return Json(new { success = true, message = "Category saved successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return Json(new { success = false, message = "Invalid category ID." });

            Guid entityId = Guid.Parse(id);
            if (entityId.Equals(Guid.Empty))
                return Json(new { success = false, message = "Invalid category ID." });

            Category category = await _baseService.GetByIdAsync(entityId);
            if (category == null)
                return Json(new { success = false, message = "Category not found." });

            category.ShowInDropDown = false;
            category.IsActive = false;

            await _baseService.DeleteAsync(category);
            await _unitOfWork.Commit();

            await _categoryCacheService.RemoveAsync();
            await _homeCacheService.RemoveAsync();
            await _categoryPageCacheService.RemoveAllAsync();

            return Json(new { success = true, message = "Category deleted successfully." });
        }
    }
}
