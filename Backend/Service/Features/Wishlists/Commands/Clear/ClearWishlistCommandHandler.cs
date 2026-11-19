using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Wishlists.Commands.Clear
{
    public sealed class ClearWishlistCommandHandler : IRequestHandler<ClearWishlistCommand, Result>
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public ClearWishlistCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
        {
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(ClearWishlistCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

            var wishlist = await _uow.Wishlists.GetByCustomerIdAsync(userId, cancellationToken);
            if (wishlist is null)
                return Result.Success();

            wishlist.Clear();

            await _uow.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
