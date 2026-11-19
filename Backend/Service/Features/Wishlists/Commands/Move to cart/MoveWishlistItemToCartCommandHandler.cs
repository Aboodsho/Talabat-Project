using Core.Results;
using Domain.Entities;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Wishlists.Commands.Move_to_cart
{
    public sealed class MoveWishlistItemToCartCommandHandler : IRequestHandler<MoveWishlistItemToCartCommand, Result>
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public MoveWishlistItemToCartCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
        {
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(MoveWishlistItemToCartCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

            var wishList= await _uow.Wishlists.GetByCustomerIdAsync(userId, cancellationToken);
            if(wishList is null || wishList.WishListItems.All(i=>i.ProductId==request.prductId))
                return Result.Failure("Item not found in wishlist.");
                
            var product= await _uow.Products.GetByIdAsync(request.prductId,cancellationToken);
            if (product is null || !product.IsActive)
                return Result.Failure("Product not found.");

            var cart = await _uow.Carts.GetByCustomerIdAsync(userId, cancellationToken);
            if (cart is null)
            { 
                cart= new Domain.Entities.Cart(userId);
                await _uow.Carts.AddAsync(cart);
            }

            cart.AddCartItem(request.prductId, 1, product.StockQuantity);
            wishList.RemoveItem(request.prductId);

            await _uow.SaveChangesAsync(cancellationToken);
            return Result.Success();

        }
    }
}
