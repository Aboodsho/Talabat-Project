using Core.Results;
using MediatR;

namespace Service.Features.Cart.Commands.RemoveCartItem
{
    public sealed record RemoveCartItemCommand(Guid productId):IRequest<Result>;

}
