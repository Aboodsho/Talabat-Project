using Domain.Entities;
using FluentValidation;

namespace Service.Features.Cart.Commands.AddItemToCart
{
    public sealed class AddItemToCartValidator:AbstractValidator<AddItemToCartCommand>
    {
        public AddItemToCartValidator()
        {
            RuleFor(x => x.productId).NotEmpty();
            RuleFor(x => x.quantity).InclusiveBetween(1, CartItem.MaxQuantity);

        }
    }
}
