using Core.Results;
using MediatR;
using Service.DTOs.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Orders.Queries.Admin.GetOrderById
{
    public sealed record  GetOrderByIdQuery(Guid OrderId):IRequest<Result<OrderDto>>;
    
}
