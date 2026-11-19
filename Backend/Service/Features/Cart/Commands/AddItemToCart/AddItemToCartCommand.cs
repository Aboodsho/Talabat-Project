using Core.Results;
using MediatR;

namespace Service.Features.Cart.Commands.AddItemToCart
{
    public sealed record AddItemToCartCommand(Guid productId,int quantity):IRequest<Result>;
    
}
