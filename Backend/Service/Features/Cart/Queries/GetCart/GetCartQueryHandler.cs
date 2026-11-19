using Core.Results;
using MediatR;
using Service.DTOs.Carts;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Cart.Queries.GetCart
{
    public class GetCartQueryHandler : IRequestHandler<GetCartQuery, Result<CartDto>>
    {

        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public GetCartQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
        {
            _uow = uow;
            _currentUser = currentUser;
        }


        public async Task<Result<CartDto>> Handle(GetCartQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
            var cart = await _uow.Carts.GetByCustomerIdAsync(userId, cancellationToken);
            if(cart is null || cart.CartItems.Count==0)
                return Result<CartDto>.Success(new CartDto(Array.Empty<CartItemDto>(), 0m));

            var products= await _uow.Products.GetByIdsAsync(
                cart.CartItems.Select(ci => ci.ProductId).ToList(), cancellationToken);

            var productMap = products.ToDictionary(p => p.Id);

            var items = new List<CartItemDto>();

            foreach (var item in cart.CartItems.OrderBy(i => i.CreatedAt))
            {
                if (!productMap.TryGetValue(item.ProductId, out var product) || !product.IsActive)
                    continue;

                var imageUrl = product.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl
                               ?? product.Images.FirstOrDefault()?.ImageUrl;

                items.Add(new CartItemDto(
                    ProductId: product.Id,
                    ProductName: product.Name,
                    ImageUrl: imageUrl,
                    UnitPrice: product.Price,
                    Quantity: item.Quantity,
                    LineTotal: product.Price * item.Quantity,
                    AvailableStock: product.StockQuantity,
                    IsAvailable: product.StockQuantity > 0));
            }
            var total = items.Where(i => i.IsAvailable).Sum(i => i.LineTotal);
            return Result<CartDto>.Success(new CartDto(items, total));



        }
    }
}
