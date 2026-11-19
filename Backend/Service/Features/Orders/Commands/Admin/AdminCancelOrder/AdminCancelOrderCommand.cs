using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Orders.Commands.Admin.AdminCancelOrder
{
    public sealed record AdminCancelOrderCommand(Guid OrderId):IRequest<Result>;
    
   
}
