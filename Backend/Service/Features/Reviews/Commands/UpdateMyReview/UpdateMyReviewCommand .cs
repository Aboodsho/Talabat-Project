using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Service.Features.Reviews.Commands.UpdateMyReview
{
    public sealed class UpdateMyReviewCommand: IRequest<Result>
    {
        [JsonIgnore] public Guid ProductId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
