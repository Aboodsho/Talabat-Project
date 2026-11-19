using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Wishlists.Commands.AddItem
{
    public sealed record  AddWishlistItemCommand(Guid productId):IRequest<Result>;
    
}
