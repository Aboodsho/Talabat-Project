using Core.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.DTOs.Reviews
{
    public sealed record ReviewDto(Guid Id, int Rating, string? Comment, DateTime CreatedAt, string AuthorName);

    public sealed record ProductReviewsDto(double AverageRating, PagedResult<ReviewDto> Reviews);
}
