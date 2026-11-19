using Core.Results;
using MediatR;
using Service.DTOs.Orders;
using Service.Interfaces;

namespace Service.Features.Orders.Queries.Admin.GetOrderById
{
    public sealed class GetOrderByIdQueryHandler(IUnitOfWork uow) : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
    {
        public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken ct)
        {
            var order = await uow.Orders.GetByIdWithDetailsAsync(request.OrderId, ct);
            if (order is null)
                return Result<OrderDto>.Failure("Order not found.");


            var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
            var names = (await uow.Products.GetByIdsIncludingDeletedAsync(productIds, ct))
                .ToDictionary(p => p.Id, p => p.Name);

            return Result<OrderDto>.Success(order.ToDto(names));

        }
    }
}
