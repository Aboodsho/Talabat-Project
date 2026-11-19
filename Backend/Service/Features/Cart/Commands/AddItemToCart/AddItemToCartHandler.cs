using Core.Results;
using MediatR;
using Service.Interfaces;
using Domain.Entities;
namespace Service.Features.Cart.Commands.AddItemToCart
{
    public sealed class AddItemToCartHandler : IRequestHandler<AddItemToCartCommand, Result>
    {
        private readonly IUnitOfWork _UOF;
        private readonly ICurrentUserService _currentUser;

        public AddItemToCartHandler(IUnitOfWork uOF, ICurrentUserService currentUser)
        {
            _UOF = uOF;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(AddItemToCartCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
            var product = await _UOF.Products.GetByIdAsync(request.productId, cancellationToken);

            if (product == null || !product.IsActive)
            {
                return Result.Failure("Product not found.");
            }

            var cart = await _UOF.Carts.GetByCustomerIdAsync(userId, cancellationToken);
            if (cart is null)
            {
                cart = new Domain.Entities.Cart(userId);
                await _UOF.Carts.AddAsync(cart, cancellationToken);
            }
            cart.AddCartItem(request.productId, request.quantity, product.StockQuantity);
            await _UOF.SaveChangesAsync(cancellationToken);
            return Result.Success();

        }
    }
}
