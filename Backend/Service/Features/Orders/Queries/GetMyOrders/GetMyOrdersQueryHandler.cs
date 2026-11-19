using Core.Pagination;
using Core.Results;
using MediatR;
using Service.DTOs.Orders;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Orders.Queries.GetMyOrders
{
    public sealed class GetMyOrdersQueryHandler : IRequestHandler<GetMyOrdersQuery, Result<PagedResult<OrderSummaryDto>>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public GetMyOrdersQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
        {
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task<Result<PagedResult<OrderSummaryDto>>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

            var page = await _uow.Orders.GetPagedByCustomerAsync(
                userId, request.PageNumber, request.PageSize, cancellationToken);

            var items = page.Items.Select(o => o.ToSummaryDto()).ToList();

            return Result<PagedResult<OrderSummaryDto>>.Success(new PagedResult<OrderSummaryDto>
                (items, page.PageNumber, page.PageSize, page.TotalCount));
        


    }
    }
}
