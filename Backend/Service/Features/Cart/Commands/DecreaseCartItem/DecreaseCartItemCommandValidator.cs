using Domain.Entities;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Cart.Commands.DecreaseCartItem
{
    public class DecreaseCartItemCommandValidator:AbstractValidator<DecreaseCartItemCommand>
    {
        public DecreaseCartItemCommandValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty();
            RuleFor(x => x.Amount).InclusiveBetween(1, CartItem.MaxQuantity);

        }
    }
}
