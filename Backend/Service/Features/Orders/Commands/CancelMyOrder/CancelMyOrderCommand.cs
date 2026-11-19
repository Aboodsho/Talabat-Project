using Core.Results;
using MediatR;

namespace Service.Features.Orders.Commands.CancelMyOrder
{
    public  sealed record CancelMyOrderCommand(Guid OrderId):IRequest<Result>;
    
}
