using Core.Results;
using MediatR;
using Service.DTOs.Carts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Cart.Queries.GetCart
{
    public sealed record GetCartQuery:IRequest<Result<CartDto>>;
    
}
