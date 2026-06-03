using AutoMapper;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AtlasCommerce.Application.Interfaces.Caching;
using AtlasCommerce.Persistance.Context;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AtlasCommerce.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]

    public class OrderController : BaseController<Order>
    {
        private readonly ILogger<OrderController> _logger;
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

        public OrderController(IBaseService<Order> baseService,
            IUnitOfWork unitOfWork,
            ILogger<OrderController> logger,
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
            _logger = logger;
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

        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
        {
            var orders = _orderService.GetPagedOrders(pageNumber, pageSize, searchTerm);

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            orders.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(orders);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var vm = _orderService.GetOrderDetails(id);

            var settingsEntity = (await _settingsService.GetAllAsync()).FirstOrDefault();
            vm.Settings = _mapper.Map<WebsiteSettingsVM>(settingsEntity);

            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(Guid orderId, enmOrderStatus status)
        {
            var order = await _baseService.GetByIdAsync(orderId);

            if (order == null)
                return NotFound();

            if (order.Status == enmOrderStatus.Cancelled ||
                order.Status == enmOrderStatus.Delivered)
                return BadRequest("Order cannot be modified.");

            bool isValid =
                (order.Status == enmOrderStatus.Paid && status == enmOrderStatus.Preparing) ||
                (order.Status == enmOrderStatus.Preparing && status == enmOrderStatus.WaitingInStore) ||
                (order.Status == enmOrderStatus.WaitingInStore && status == enmOrderStatus.Delivered);
            
            if (!isValid)
            {
                return BadRequest(new
                {
                    message = $"You cannot move order from '{order.Status}' to '{status}'. Please follow the correct order workflow.",
                    currentStatus = order.Status.ToString(),
                    targetStatus = status.ToString()
                });
            }

            order.Status = status;

            await _baseService.UpdateAsync(order);
            await _unitOfWork.Commit();

            return Ok(new
            {
                message = "Order status updated successfully.",
                newStatus = status.ToString()
            });
        }
    }
}
