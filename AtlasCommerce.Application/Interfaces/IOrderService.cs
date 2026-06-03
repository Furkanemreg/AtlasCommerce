using AtlasCommerce.Application.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Interfaces
{
    public interface IOrderService
    {
        PagedResult<OrderSummaryVM> GetPagedOrders(int pageNumber, int pageSize, string? searchTerm = null);
        OrderDetailsVM GetOrderDetails(Guid orderId);
    }
}
