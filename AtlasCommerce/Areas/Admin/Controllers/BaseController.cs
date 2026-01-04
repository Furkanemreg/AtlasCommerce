using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace AtlasCommerce.UI.Areas.Admin.Controllers
{
    public class BaseController<T> : Controller where T : BaseEntity
    {
        protected readonly IBaseService<T> _baseService;
        protected readonly IUnitOfWork _unitOfWork;

        public BaseController(IBaseService<T> baseService, IUnitOfWork unitOfWork)
        {
            _baseService = baseService;
            _unitOfWork = unitOfWork;
        }

    }

    public class BaseController : Controller
    {
        public BaseController()
        {

        }
    }
}
