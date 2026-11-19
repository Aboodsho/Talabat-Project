using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Wishlists.Commands.Move_to_cart
{
    public sealed class MoveWishlistItemToCartCommandValidator:AbstractValidator<MoveWishlistItemToCartCommand>
    {
        public MoveWishlistItemToCartCommandValidator()
        {
            RuleFor(x=>x.prductId).NotEmpty();
        }
    }
}
