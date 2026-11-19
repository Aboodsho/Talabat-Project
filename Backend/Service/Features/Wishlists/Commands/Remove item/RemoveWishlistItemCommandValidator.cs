using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Wishlists.Commands.Remove_item
{
    public sealed class RemoveWishlistItemCommandValidator:AbstractValidator<RemoveWishlistItemCommand>
    {
        public RemoveWishlistItemCommandValidator()
        {
            RuleFor(x => x.productId).NotEmpty();
        }
    }
}
