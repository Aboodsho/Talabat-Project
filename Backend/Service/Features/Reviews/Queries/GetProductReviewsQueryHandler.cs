using Core.Pagination;
using Core.Results;
using MediatR;
using Service.DTOs.Reviews;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Reviews.Queries
{
    public sealed class GetProductReviewsQueryHandler(IUnitOfWork uow, IUserLookupService userLookup)
            : IRequestHandler<GetProductReviewsQuery, Result<ProductReviewsDto>>

    {
        public async Task<Result<ProductReviewsDto>> Handle(GetProductReviewsQuery request, CancellationToken ct)
        {
            var products = await uow.Products.GetByIdsAsync(new[] { request.ProductId }, ct);
            if (products.Count == 0)
                return Result<ProductReviewsDto>.Failure("Product not found.");

            var page = await uow.Reviews.GetPagedByProductAsync(request.ProductId, request.PageNumber, request.PageSize, ct);
            var average = await uow.Reviews.GetAverageRatingAsync(request.ProductId, ct);

            var names = await userLookup.GetNamesByIdsAsync(page.Items.Select(r => r.CustomerId), ct);   // ONE query

            var items = page.Items
                .Select(r => new ReviewDto(
                    r.Id, r.Rating, r.Comment, r.CreatedAt,
                    names.GetValueOrDefault(r.CustomerId, "Deleted user")))
                .ToList();

            var paged = new PagedResult<ReviewDto>(items, page.PageNumber, page.PageSize, page.TotalCount);
            return Result<ProductReviewsDto>.Success(new ProductReviewsDto(average, paged));

        }
    }
}
