using AutoMapper;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using AtlasCommerce.Persistance.Services;
using AtlasCommerce.UI.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace AtlasCommerce.UI.Controllers.Contact
{
    [AllowAnonymous]
    public class ContactController : BaseController<UserMessage>
    {
        private readonly ILogger<ContactController> _logger;
        private readonly IBaseService<Category> _categoryService;
        private readonly IBaseService<Product> _productService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IImageService _imageService;
        private readonly IBaseService<WebsiteSettings> _settingsService;
        private readonly IMapper _mapper;

        public ContactController(IBaseService<UserMessage> baseService, IUnitOfWork unitOfWork, IBaseService<Category> categoryService, UserManager<AppUser> userManager, IBaseService<Product> productService, IImageService imageService,
        ILogger<ContactController> logger,
        IBaseService<WebsiteSettings> settingsService,
        IMapper mapper)
            : base(baseService, unitOfWork)
        {
            _categoryService = categoryService;
            _productService = productService;
            _imageService = imageService;
            _logger = logger;
            _userManager = userManager;
            _settingsService = settingsService;
            _mapper = mapper;
        }

        private async Task LoadProductsToDropdown()
        {
            // Navbardaki her kategori için ürünlerini getir
            var dropdownCategories = await _categoryService.GetAllAsync(x => x.ShowInDropDown == true && x.IsActive == true);
            var categoryWithProducts = new List<CategoryWithProductsVM>();

            foreach (var cat in dropdownCategories)
            {
                var products = await _productService.GetAllAsync(p => p.CategoryId == cat.Id && p.IsActive);
                var productVMs = new List<SaleProductVM>();

                foreach (var p in products)
                {
                    string? imageDataUri = null;
                    var img = await _imageService.GetByOwnerAsync(p.Id, nameof(Product));
                    if (img?.Data != null)
                        imageDataUri = $"data:image/png;base64,{Convert.ToBase64String(img.Data)}";

                    productVMs.Add(new SaleProductVM
                    {
                        Id = p.Id,
                        Title = p.Name!,
                        Barcode = p.Barcode!,
                        SalePrice = p.SalePriceIncludingTaxes,
                        CategoryId = p.CategoryId
                    });
                }

                categoryWithProducts.Add(new CategoryWithProductsVM
                {
                    Id = cat.Id,
                    Name = cat.Name!,
                    Products = productVMs
                });
            }

            ViewBag.CategoryWithProducts = categoryWithProducts;
        }

        private void SetCart()
        {
            var sessionCart = HttpContext.Session.GetString("CartSession");
            ViewBag.CartItemCount = !string.IsNullOrEmpty(sessionCart)
                ? JsonConvert.DeserializeObject<List<CartItemVM>>(sessionCart)!.Sum(x => x.Quantity)
                : 0;
        }
        
        public async Task<IActionResult> Index()
        {
            await LoadProductsToDropdown();
            SetCart();

            var vm = new UserMessageVM() { EmailAddress = string.Empty, Message = string.Empty, Topic = string.Empty };

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(vm);
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Route("Contact/SendMessageAsync")]
        //public async Task<IActionResult> SendMessageAsync([FromForm] UserMessageVM messageVM)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        await LoadProductsToDropdown();
        //        SetCart();
        //        return View("Index", messageVM);
        //    }

        //    try
        //    {
        //        var message = new UserMessage
        //        {
        //            EmailAddress = "",
        //            Topic = messageVM.Topic,
        //            Message = messageVM.Message,
        //        };

        //        if (User.Identity?.IsAuthenticated ?? false)
        //        {
        //            // Giriş yapmış kullanıcı bilgilerini otomatik al
        //            var user = await _userManager.GetUserAsync(User);
        //            if (user != null)
        //            {
        //                message.UserId = user.Id;
        //                message.Name = user.FirstName;
        //                message.Surname = user.LastName;
        //                message.EmailAddress = user.Email;
        //                message.PhoneNumber = user.PhoneNumber;
        //            }
        //        }
        //        else
        //        {
        //            // "Giriş yapmamış kullanıcı" formdan bilgilerini girer
        //            message.Name = messageVM.Name;
        //            message.Surname = messageVM.Surname;
        //            message.EmailAddress = messageVM.EmailAddress;
        //            message.PhoneNumber = messageVM.PhoneNumber;
        //        }

        //        await _baseService.AddAsync(message);
        //        await _unitOfWork.Commit();

        //        TempData["SuccessMessage"] = "Mesajınız başarıyla gönderildi.";
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Mesaj gönderilirken hata oluştu.");
        //        TempData["ErrorMessage"] = "Mesaj gönderilirken bir hata oluştu. Lütfen tekrar deneyin.";
        //    }

        //    return RedirectToAction("Index");
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Contact/SendMessageAsync")]
        public async Task<IActionResult> SendMessageAsync(UserMessageVM messageVM)
        {
            if (!ModelState.IsValid)
            {
                await LoadProductsToDropdown();
                SetCart();
                return View("Index", messageVM);
            }

            try
            {
                var message = new UserMessage
                {
                    EmailAddress = "",
                    Topic = messageVM.Topic,
                    Message = messageVM.Message,
                };

                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await _userManager.GetUserAsync(User);

                    if (user == null)
                        return Unauthorized();

                    // SADECE SERVER VERİSİ
                    message.UserId = user.Id;
                    message.Name = user.FirstName;
                    message.Surname = user.LastName;
                    message.EmailAddress = user.Email;
                    message.PhoneNumber = user.PhoneNumber;
                }
                else
                {
                    // Guest User formdan gelir
                    message.Name = messageVM.Name;
                    message.Surname = messageVM.Surname;
                    message.EmailAddress = messageVM.EmailAddress;
                    message.PhoneNumber = messageVM.PhoneNumber;
                }

                await _baseService.AddAsync(message);
                await _unitOfWork.Commit();

                TempData["SuccessMessage"] = "Mesajınız başarıyla gönderildi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mesaj gönderilirken hata oluştu.");
                TempData["ErrorMessage"] = "Mesaj gönderilirken bir hata oluştu.";
            }

            return RedirectToAction("Index");
        }
    }
}
