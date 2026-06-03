using AutoMapper;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AtlasCommerce.Application.Interfaces.Caching;
using AtlasCommerce.Persistance.Context;

namespace AtlasCommerce.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PaymentController : BaseController<Payment>
    {
        private readonly IMapper _mapper;
        private readonly IAccountService _accountService;
        private readonly IImageService _imageService;
        private readonly IBaseService<Image> _imageBaseService;
        private readonly IBaseService<Category> _categoryService;
        private readonly IBaseService<WebsiteSettings> _settingsService;
        private readonly ApplicationDbContext _context;
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly IProductListCacheService _productListCacheService;

        public PaymentController(IBaseService<Payment> baseService,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IAccountService accountService,
            IImageService imageService,
            IBaseService<Image> imageBaseService,
            IBaseService<Category> categoryService,
            ApplicationDbContext context,
            IProductService productService,
            IOrderService orderService,
            IBaseService<WebsiteSettings> settingsService,
            IProductListCacheService productListCacheService) : base(baseService, unitOfWork)
        {
            _mapper = mapper;
            _accountService = accountService;
            _imageService = imageService;
            _imageBaseService = imageBaseService;
            _categoryService = categoryService;
            _context = context;
            _productService = productService;
            _orderService = orderService;
            _settingsService = settingsService;
            _productListCacheService = productListCacheService;
        }

        public async Task<IActionResult> Index()
        {
            var payments = await _baseService.GetAllAsync(x => x.Order);

            var vm = new PaymentListVM
            {
                Payments = payments
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(p => new PaymentListItemVM
                    {
                        Id = p.Id,
                        Amount = p.Amount,
                        Status = p.Status,
                        CreatedAt = p.CreatedAt,
                        PaidAt = p.PaidAt,

                        CardBrand = p.CardBrand,
                        CardNumber = p.CardNumber,

                        OrderId = p.OrderId,
                        OrderNumber = p.Order?.OrderNumber,
                        OrderTotal = p.Order?.TotalAmount ?? 0
                    })
                    .ToList(),

                Settings = _mapper.Map<WebsiteSettingsVM>(
                    (await _settingsService.GetAllAsync()).FirstOrDefault()
                )
            };

            return View(vm);
        }

        public async Task<IActionResult> Detail(Guid id)
        {
            var payment = (await _baseService.GetAllAsync(x => x.Order))
                .FirstOrDefault(x => x.Id == id);

            if (payment == null)
                return NotFound();

            var vm = new PaymentDetailVM
            {
                Id = payment.Id,

                Amount = payment.Amount,
                Status = payment.Status,
                CreatedAt = payment.CreatedAt,
                PaidAt = payment.PaidAt,

                CardBrand = payment.CardBrand,
                CardNumber = payment.CardNumber,

                OrderId = payment.OrderId,
                OrderNumber = payment.Order?.OrderNumber,
                OrderTotal = payment.Order?.TotalAmount ?? 0,

                Settings = _mapper.Map<WebsiteSettingsVM>(
                    (await _settingsService.GetAllAsync()).FirstOrDefault()
                )
            };

            return View(vm);
        }
    }
}
