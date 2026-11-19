using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Reviews.Commands.AdminDeleteReview
{
    public sealed class AdminDeleteReviewCommandValidator : AbstractValidator<AdminDeleteReviewCommand>
    {
        public AdminDeleteReviewCommandValidator() => RuleFor(x => x.ReviewId).NotEmpty();

    }
}
