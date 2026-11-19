using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Reviews.Commands.AdminDeleteReview
{
    public sealed record AdminDeleteReviewCommand(Guid ReviewId) : IRequest<Result>;

}
