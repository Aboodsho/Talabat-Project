using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Wishlists.Commands.Move_to_cart
{
    public sealed record MoveWishlistItemToCartCommand(Guid prductId):IRequest<Result>;
    
}
