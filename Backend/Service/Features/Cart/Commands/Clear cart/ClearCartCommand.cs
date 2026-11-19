using Core.Results;
using MediatR;

namespace Service.Features.Cart.Commands.Clear_cart
{
    public class ClearCartCommand:IRequest<Result>;
}
