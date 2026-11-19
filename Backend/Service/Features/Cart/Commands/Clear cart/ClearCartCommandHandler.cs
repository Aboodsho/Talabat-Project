using Core.Results;
using MediatR;
using Service.Interfaces;

namespace Service.Features.Cart.Commands.Clear_cart
{
    public sealed record  ClearCartCommandHandler : IRequestHandler<ClearCartCommand, Result>
    {

        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public ClearCartCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
        {
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(ClearCartCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

            var cart = await _uow.Carts.GetByCustomerIdAsync(userId, cancellationToken);
            if (cart is null)
                return Result.Success();

            cart.Clear();
            await _uow.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

}
