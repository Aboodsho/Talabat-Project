using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Reviews.Queries
{
    public sealed class GetProductReviewsQueryValidator : AbstractValidator<GetProductReviewsQuery>
    {
        public GetProductReviewsQueryValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty();
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        }
    }
}
