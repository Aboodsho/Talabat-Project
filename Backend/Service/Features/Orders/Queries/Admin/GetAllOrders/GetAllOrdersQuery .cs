using Core.Pagination;
using Core.Results;
using Domain.Enums;
using MediatR;
using Service.DTOs.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Orders.Queries.Admin.GetAllOrders
{
    public sealed class GetAllOrdersQuery:IRequest<PagedResult<OrderSummaryDto>>
    {
        public OrderStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
