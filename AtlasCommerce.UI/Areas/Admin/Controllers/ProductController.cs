using AutoMapper;
using AtlasCommerce.Application.Extensions;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using AtlasCommerce.Persistance.Context;
using AtlasCommerce.UI.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AtlasCommerce.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductController : BaseController<Product>
    {
        private readonly IMapper _mapper;
        private readonly IAccountService _accountService;
        private readonly IImageService _imageService;
        private readonly IBaseService<Image> _imageBaseService;
        private readonly IBaseService<Category> _categoryService;
        private readonly IBaseService<WebsiteSettings> _settingsService;
        private readonly ApplicationDbContext _context;
        private readonly IProductService _productService;

        public ProductController(IBaseService<Product> baseService, IUnitOfWork unitOfWork, IMapper mapper, IAccountService accountService, IImageService imageService, IBaseService<Image> imageBaseService, IBaseService<Category> categoryService, ApplicationDbContext context, IProductService productService, IBaseService<WebsiteSettings> settingsService) : base(baseService, unitOfWork)
        {
            _mapper = mapper;
            _accountService = accountService;
            _imageService = imageService;
            _imageBaseService = imageBaseService;
            _categoryService = categoryService;
            _context = context;
            _productService = productService;
            _settingsService = settingsService;
        }

        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
        {
            PagedResult<ProductListVM> products = _productService.GetPagedProducts(pageNumber, pageSize, searchTerm);
            foreach(var product in products.Items)
            {
                if (product.Data != null)
                {
                    var ext = Path.GetExtension(product.FileName)?.TrimStart('.').ToLower();
                    var mime = GetMime(ext);
                    product.ImageDataUri = $"data:{mime};base64,{Convert.ToBase64String(product.Data)}";
                }
            }

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            products.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id = "")
        {
            ProductVM vm;
            var categories = await _categoryService.GetAllAsync();
            if (string.IsNullOrEmpty(id) || Guid.Parse(id) == Guid.Empty)
            {
                vm = new ProductVM
                {
                    Colors = EnumHelper.GetEnumSelectListWithDescription<enmColors>(),
                    CreatedUser = await _accountService.GetByUserName(User.Identity?.Name),
                    CategoryOptions = categories.Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    })
                };
            }
            else
            {
                var guid = Guid.Parse(id);
                var product = await _baseService.GetByIdAsync(guid);
                product.Images = await _imageService.GetAllByOwnerAsync(product.Id, nameof(Product));
                vm = _mapper.Map<ProductVM>(product);
                vm.Colors = EnumHelper.GetEnumSelectListWithDescription<enmColors>();

                vm.CreatedUser = await _accountService.GetByIdAsync(vm.CreatedBy.ToString());
                if (vm.UpdatedBy != null && vm.UpdatedBy != Guid.Empty)
                    vm.UpdatedUser = await _accountService.GetByIdAsync(vm.UpdatedBy.ToString());

                if (vm.Images.Count > 0)
                {
                    vm.Images = product.Images.Select(img => new ImageVM
                    {
                        Id = img.Id,
                        FileName = img.FileName,
                        DataUri = $"data:{GetMime(img.FileName)};base64,{Convert.ToBase64String(img.Data)}"
                    }).ToList();
                }

                if (product.MainPhotoId != Guid.Empty)
                {
                    var mainPhotoIdx = product.Images
                        .Select((img, idx) => new { img.Id, Index = idx })
                        .FirstOrDefault(x => x.Id == product.MainPhotoId)?.Index;

                    if (mainPhotoIdx.HasValue)
                        vm.MainPhotoIndex = mainPhotoIdx.Value;
                }

                vm.CategoryOptions = categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = (c.Id == vm.CategoryId)
                });
            }

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductVM vm)
         {
            if (!ModelState.IsValid)
            {
                await PopulateCategoryOptions(vm);
                await PopulateExistingImages(vm);
                return View(vm);
            }

            var isNew = vm.Id == Guid.Empty;
            Product product;

            // 1. Map & Kaydet (yeni ürünse hemen id’yi alalım)
            if (isNew)
            {
                product = _mapper.Map<Product>(vm);
                await _baseService.AddAsync(product);
                await _unitOfWork.Commit();      // ← product.Id şimdi gerçek
            }
            else
            {
                product = await _baseService.GetByIdAsync(vm.Id);
                if (product == null)
                    return Json(new { success = false, message = "Seçilen ürün bulunamadı." });

                _mapper.Map(vm, product);
                // Eski resimleri, silme-kept işlemleri öncesi getirelim
            }

            // 2. Var olan resimler arasından silinecekleri kaldır
            var keepIds = vm.ExistingImageIds;
            var allImages = (await _imageService.GetAllByOwnerAsync(product.Id, nameof(Product))).ToList();

            var toDelete = allImages.Where(i => !keepIds.Contains(i.Id)).ToList();
            if (toDelete.Any())
                await _imageBaseService.RemoveRangeAsync(toDelete);

            // Kalan resimleri product.Images’a ata
            var keptImages = allImages.Except(toDelete).ToList();
            product.Images = keptImages;

            // 3. Yeni yüklenecek dosyalar varsa ekle
            if (vm.UploadFiles?.Any() == true)
            {
                var newImages = new List<Image>();
                foreach (var file in vm.UploadFiles)
                {
                    if (file.Length <= 0 || !file.ContentType.StartsWith("image/"))
                        continue;

                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);

                    newImages.Add(new Image
                    {
                        OwnerId = product.Id,               // ← artık gerçek GUID
                        OwnerType = nameof(Product),
                        FileName = file.FileName,
                        Data = ms.ToArray(),
                        FileSize = file.Length
                    });
                }

                if (newImages.Any())
                {
                    await _imageBaseService.AddRangeAsync(newImages);
                    // isNew senaryosunda product.Images zaten boştu; güncelleyelim
                    product.Images = product.Images.Concat(newImages).ToList();
                }
            }

            // 4. MainPhotoId ayarları
            if (vm.MainPhotoId.HasValue)
            {
                product.MainPhotoId = vm.MainPhotoId.Value;
            }
            else if (vm.MainPhotoIndex.HasValue)
            {
                var list = product.Images.ToList();
                if (vm.MainPhotoIndex.Value >= 0 && vm.MainPhotoIndex.Value < list.Count)
                    product.MainPhotoId = list[vm.MainPhotoIndex.Value].Id;
            }

            // 5. Son güncelleme ve commit
            if (!isNew)
                await _baseService.UpdateAsync(product);
            else
                // isNew ise zaten başta Add+Commit yaptık, sadece Update ederek MainPhotoId vs. kaydedelim
                await _baseService.UpdateAsync(product);

            await _unitOfWork.Commit();

            return Json(new { success = true, message = "Ürün başarıyla kaydedildi." });
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(ProductVM vm)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        await PopulateCategoryOptions(vm);
        //        await PopulateExistingImages(vm);
        //        return View(vm);
        //    }

        //    var isNew = vm.Id == Guid.Empty;
        //    Product? product;

        //    if (isNew)
        //    {
        //        product = _mapper.Map<Product>(vm);
        //        product.Images = await _imageService.GetAllByOwnerAsync(product.Id, nameof(Product));
        //    }
        //    else
        //    {
        //        product = await _baseService.GetByIdAsync(vm.Id);
        //        if (product == null)
        //            return Json(new { success = false, message = "Seçilen ürün bulunamadı." });
        //        _mapper.Map(vm, product);
        //        product.Images = await _imageService.GetAllByOwnerAsync(product.Id, nameof(Product));
        //    }

        //    var keepIds = vm.ExistingImageIds;
        //    var allImages = (await _imageService.GetAllByOwnerAsync(product.Id, nameof(Product))).ToList();

        //    var toDelete = allImages.Where(i => !keepIds.Contains(i.Id)).ToList();
        //    if (toDelete.Any()) await _imageBaseService.RemoveRangeAsync(toDelete);

        //    var keptImages = allImages.Where(i => keepIds.Contains(i.Id)).ToList();
        //    product.Images = keptImages;

        //    if (vm.UploadFiles?.Any() == true)
        //    {
        //        var newImages = new List<Image>();
        //        foreach (var file in vm.UploadFiles)
        //        {
        //            if (file.Length <= 0 || !file.ContentType.StartsWith("image/")) continue;
        //            using var ms = new MemoryStream();
        //            await file.CopyToAsync(ms);
        //            newImages.Add(new Image
        //            {
        //                OwnerId = product.Id,
        //                OwnerType = nameof(Product),
        //                FileName = file.FileName,
        //                Data = ms.ToArray(),
        //                FileSize = file.Length
        //            });
        //        }
        //        await _imageBaseService.AddRangeAsync(newImages);
        //        product.Images = product.Images.Concat(newImages).ToList();
        //    }

        //    if (vm.MainPhotoId.HasValue)
        //    {
        //        product.MainPhotoId = vm.MainPhotoId.Value;
        //    }
        //    else if (vm.MainPhotoIndex.HasValue)
        //    {
        //        var imageList = product.Images.ToList();
        //        if (vm.MainPhotoIndex.Value >= 0 && vm.MainPhotoIndex.Value < imageList.Count)
        //            product.MainPhotoId = imageList[vm.MainPhotoIndex.Value].Id;
        //    }

        //    if (isNew) await _baseService.AddAsync(product);
        //    else await _baseService.UpdateAsync(product);

        //    await _unitOfWork.Commit();
        //    return Json(new { success = true, message = "Ürün başarıyla kaydedildi." });
        //}

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return Json(new { success = false, message = "Geçersiz ürün ID." });

            Guid entityId = Guid.Parse(id);
            if (entityId.Equals(Guid.Empty))
                return Json(new { success = false, message = "Geçersiz ürün ID." });

            Product product = await _baseService.GetByIdAsync(entityId);
            if (product == null)
                return Json(new { success = false, message = "Ürün bulunamdı." });

            await _baseService.DeleteAsync(product);
            await _unitOfWork.Commit();

            return Json(new { success = true, message = "Ürün başarıyla silindi." });
        }

        [NonAction]
        private string GetMime(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.TrimStart('.').ToLower();
            return ext switch
            {
                "png" => "image/png",
                "jpg" or "jpeg" => "image/jpeg",
                "gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }

        [NonAction]
        private async Task PopulateCategoryOptions(ProductVM vm)
        {
            var cats = await _categoryService.GetAllAsync();
            vm.CategoryOptions = cats.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = vm.CategoryId == c.Id
            });
        }

        [NonAction]
        private async Task PopulateExistingImages(ProductVM vm)
        {
            if (vm.Id == Guid.Empty) return;
            var product = await _context.Products
                            .Include(p => p.Images)
                            .FirstOrDefaultAsync(p => p.Id == vm.Id);
            vm.Images = product.Images.Select(img => new ImageVM
            {
                Id = img.Id,
                FileName = img.FileName,
                DataUri = $"data:{GetMime(img.FileName)};base64,{Convert.ToBase64String(img.Data)}",
                IsMain = img.Id == vm.MainPhotoId
            }).ToList();
        }
    }
}
