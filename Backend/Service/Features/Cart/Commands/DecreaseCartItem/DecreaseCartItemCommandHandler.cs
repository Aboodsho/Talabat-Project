using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Cart.Commands.DecreaseCartItem
{
    public class DecreaseCartItemCommandHandler : IRequestHandler<DecreaseCartItemCommand, Result>
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public DecreaseCartItemCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
        {
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(DecreaseCartItemCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

            var cart = await _uow.Carts.GetByCustomerIdAsync(userId, cancellationToken);
            if (cart is null)
                return Result.Failure("Cart not found.");

            cart.DecreaseItemQuantity(request.ProductId, request.Amount);

            await _uow.SaveChangesAsync(cancellationToken);
            return Result.Success();


        }
    }
}
