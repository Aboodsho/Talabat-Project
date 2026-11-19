using Core.Results;
using MediatR;
using Service.DTOs.Wishlists;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Wishlists.Queries.Get_wishlist
{
    public sealed record GetWishlistQuery:IRequest<Result<WishlistDto>>;
    
}
