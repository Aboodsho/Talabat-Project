using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Orders.Commands.Admin.UpdateOrderStatus
{
    public record UpdateOrderStatusCommand(Guid OrderId, OrderAction Action):IRequest<Result>;
    
   
}
