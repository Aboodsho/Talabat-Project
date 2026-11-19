using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Wishlists.Commands.Remove_item
{
    public sealed record RemoveWishlistItemCommand(Guid productId):IRequest<Result>;
    
}
