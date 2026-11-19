using Core.Results;
using Domain.Entities;
using MediatR;
using Service.DTOs.Wishlists;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Wishlists.Queries.Get_wishlist
{
    public sealed class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, Result<WishlistDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public GetWishlistQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
        {
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task<Result<WishlistDto>> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

            var wishList= await _uow.Wishlists.GetByCustomerIdAsync(userId);
            if (wishList is null || wishList.WishListItems.Count == 0)
                return Result<WishlistDto>.Success(new WishlistDto(Array.Empty<WishlistItemDto>(), 0));

            var products = await _uow.Products.GetByIdsAsync(
                wishList.WishListItems.Select(i => i.ProductId),
                cancellationToken);
            var productMap = products.ToDictionary(p => p.Id);

            var items = new List<WishlistItemDto>();

            foreach (var item in wishList.WishListItems.OrderByDescending(i => i.CreatedAt))
            {
                if (!productMap.TryGetValue(item.ProductId, out var product) || !product.IsActive)
                    continue;
                var imageUrl = product.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl
                                            ?? product.Images.FirstOrDefault()?.ImageUrl;

                items.Add(new WishlistItemDto(
                   product.Id,
                   product.Name,
                   imageUrl,
                   product.Price,
                   product.StockQuantity > 0,
                   item.CreatedAt));
            }
            return Result<WishlistDto>.Success(new WishlistDto(items, items.Count));

        }
    }
}
