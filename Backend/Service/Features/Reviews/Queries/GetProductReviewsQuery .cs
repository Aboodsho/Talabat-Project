using Core.Results;
using MediatR;
using Service.DTOs.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Service.Features.Reviews.Queries
{
    public sealed class GetProductReviewsQuery : IRequest<Result<ProductReviewsDto>>
    {
        [JsonIgnore] public Guid ProductId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

}
