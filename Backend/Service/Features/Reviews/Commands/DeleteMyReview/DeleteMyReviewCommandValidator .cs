using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Reviews.Commands.DeleteMyReview
{
    internal class DeleteMyReviewCommandValidator: AbstractValidator<DeleteMyReviewCommand>
    {
        public DeleteMyReviewCommandValidator() => RuleFor(x => x.ProductId).NotEmpty();

    }
}
