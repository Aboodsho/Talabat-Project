using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Orders.Queries.GetMyOrders
{
    public sealed class GetMyOrdersQueryValidator : AbstractValidator<GetMyOrdersQuery>
    {
        public GetMyOrdersQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        }
    }
}
