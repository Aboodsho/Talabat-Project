using Core.Pagination;
using Core.Results;
using MediatR;
using Service.DTOs.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Orders.Queries.GetMyOrders
{
    public sealed class GetMyOrdersQuery:IRequest<Result<PagedResult<OrderSummaryDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
