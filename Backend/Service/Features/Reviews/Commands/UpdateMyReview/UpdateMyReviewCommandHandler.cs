using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Reviews.Commands.UpdateMyReview
{
    public sealed class UpdateMyReviewCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser) : IRequestHandler<UpdateMyReviewCommand, Result>
    {
        public async Task<Result> Handle(UpdateMyReviewCommand request, CancellationToken ct)
        {
            var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

            var review = await uow.Reviews.GetByProductAndCustomerAsync(request.ProductId, userId, ct);
            if (review is null)
                return Result.Failure("Review not found.");       // not mine = not found

            review.SetRating(request.Rating);
            review.UpdateComment(request.Comment);

            await uow.SaveChangesAsync(ct);
            return Result.Success();
        }
    }
}
