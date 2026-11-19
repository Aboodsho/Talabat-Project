using Core.Results;
using FluentValidation;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Reviews.Commands.AdminDeleteReview
{
    public sealed class AdminDeleteReviewCommandHandler(IUnitOfWork uow)
        : IRequestHandler<AdminDeleteReviewCommand, Result>
    {
        public async Task<Result> Handle(AdminDeleteReviewCommand request, CancellationToken ct)
        {
            var review = await uow.Reviews.GetByIdAsync(request.ReviewId, ct);   // generic, tracked
            if (review is null)
                return Result.Failure("Review not found.");

            uow.Reviews.Remove(review);
            await uow.SaveChangesAsync(ct);
            return Result.Success();
        }
    }
}
