using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Wishlists.Commands.AddItem
{
    public sealed class AddWishlistItemCommandHandler : IRequestHandler<AddWishlistItemCommand, Result>

    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUserService;

        public AddWishlistItemCommandHandler(IUnitOfWork uow, ICurrentUserService currentUserService)
        {
            _uow = uow;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(AddWishlistItemCommand request, CancellationToken cancellationToken)
        {
            var userId=_currentUserService.UserId?? throw new UnauthorizedAccessException();    
            var product= await _uow.Products.GetByIdAsync(request.productId);
            if (product is null || !product.IsActive)
                return Result.Failure("Product not found.");

            var wishList = await _uow.Wishlists.GetByCustomerIdAsync(userId, cancellationToken);
            if (wishList is null)
            { 
                wishList=new Domain.Entities.Wishlist(userId);
                await _uow.Wishlists.AddAsync(wishList);
            }

            wishList.AddItem(request.productId);

            await _uow.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
