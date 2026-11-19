using Core.Pagination;
using MediatR;
using Service.DTOs.Orders;
using Service.Interfaces;

namespace Service.Features.Orders.Queries.Admin.GetAllOrders
{
    public class GetAllOrdersQueryHandler(IUnitOfWork uow) :
        IRequestHandler<GetAllOrdersQuery, PagedResult<OrderSummaryDto>>
    {
        public async Task<PagedResult<OrderSummaryDto>> Handle(GetAllOrdersQuery request, CancellationToken ct)
        {
            var page = await uow.Orders.GetPagedAsync(request.Status, request.PageNumber, request.PageSize, ct);
            var items = page.Items.Select(o => o.ToSummaryDto()).ToList();

            return new PagedResult<OrderSummaryDto>(items, page.PageNumber, page.PageSize, page.TotalCount);

        }
    }
}
