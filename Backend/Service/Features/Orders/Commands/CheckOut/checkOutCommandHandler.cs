using Core.Results;
using Domain.Entities;
using MediatR;
using Service.Interfaces;

namespace Service.Features.Orders.Commands.CheckOut
{
    public sealed class checkOutCommandHandler : IRequestHandler<checkOutCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public checkOutCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
        {
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(checkOutCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

            var cart= await _uow.Carts.GetByCustomerIdAsync(userId, cancellationToken);
            if (cart is null || cart.CartItems.Count == 0)
                return Result<Guid>.Failure("Cart is empty.");

            var products = await _uow.Products.GetByIdsTrackedAsync(cart.CartItems.Select(i=>i.ProductId),cancellationToken);
            var productMap = products.ToDictionary(p => p.Id);

            var orderItems = new List<(Guid prodcutid, int quantity, decimal unitprice)>();

            foreach (var cartItem in cart.CartItems)
            {
                if (!productMap.TryGetValue(cartItem.ProductId, out var product))
                    return Result<Guid>.Failure(
                        $"A product in your cart no longer exists (id: {cartItem.ProductId}). Please review your cart.");

                if (!product.IsActive)
                    return Result<Guid>.Failure(
                        $"'{product.Name}' is no longer available. Please review your cart.");

                if (product.StockQuantity < cartItem.Quantity)
                    return Result<Guid>.Failure(
                        $"Not enough stock for '{product.Name}'. Available: {product.StockQuantity}.");

                product.DecreaseStock(cartItem.Quantity);
                orderItems.Add((product.Id,cartItem.Quantity, product.Price));

            }

            var order = new Order( userId, orderItems,
                request.street.Trim(), request.City.Trim(),
                request.state?.Trim(), request.PostalCode?.Trim(),
                request.Country.Trim()
                );

            await _uow.Orders.AddAsync(order, cancellationToken);
            cart.Clear();

            await _uow.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(order.Id);




        }
    }
}
