using Core.Results;
using MediatR;
using Service.DTOs.Orders;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Service.Features.Orders.Queries.GetOrderById
{
    public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public GetOrderByIdQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
        {
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

            var order = await _uow.Orders.GetByIdWithDetailsAsync(request.OrderId, cancellationToken);
            if (order is null || order.CustomerId != userId)
                return Result<OrderDto>.Failure("Order not found.");

            var products = await _uow.Products.GetByIdsIncludingDeletedAsync(
                order.Items.Select(i => i.ProductId), cancellationToken);
            var names = products.ToDictionary(p => p.Id, p => p.Name);

            return Result<OrderDto>.Success(order.ToDto(names));
        }
    }
}
