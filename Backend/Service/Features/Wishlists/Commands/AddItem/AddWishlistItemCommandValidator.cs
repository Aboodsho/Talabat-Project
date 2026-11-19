using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Wishlists.Commands.AddItem
{
    public sealed class AddWishlistItemCommandValidator:AbstractValidator<AddWishlistItemCommand>
    {
        public AddWishlistItemCommandValidator()
        {
            RuleFor(x => x.productId).NotEmpty();

        }
    }
}
