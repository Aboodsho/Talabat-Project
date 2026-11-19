using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Reviews.Commands.DeleteMyReview
{
    public sealed class DeleteMyReviewCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
        : IRequestHandler<DeleteMyReviewCommand, Result>
    {
        public async  Task<Result> Handle(DeleteMyReviewCommand request, CancellationToken ct)
        {
            var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

            var review = await uow.Reviews.GetByProductAndCustomerAsync(request.ProductId, userId, ct);
            if (review is null)
                return Result.Failure("Review not found.");

            uow.Reviews.Remove(review);
            await uow.SaveChangesAsync(ct);
            return Result.Success();
        }
    }
}
