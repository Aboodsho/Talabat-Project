using Core.Results;
using Domain.Entities;
using Domain.Exceptions;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Reviews.Commands.AddReview
{
    public sealed class AddReviewCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
        : IRequestHandler<AddReviewCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(AddReviewCommand request, CancellationToken ct)
        {
            var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

            var product = await uow.Products.GetByIdAsync(request.ProductId, ct);   // deleted = null (query filter)
            if (product is null)
                return Result<Guid>.Failure("Product not found.");

            var purchased = await uow.Orders.HasCustomerPurchasedProductAsync(userId, request.ProductId, ct);
            if (!purchased)
                throw new DomainException("review.not_purchased", "You can review only products from delivered orders.");

            var existing = await uow.Reviews.GetByProductAndCustomerAsync(request.ProductId, userId, ct);
            if (existing is not null)
                throw new DomainException("review.already_exists", "You already reviewed this product. Update your review instead.");

            var review = new Review(request.ProductId, userId, request.Rating, request.Comment);

            await uow.Reviews.AddAsync(review, ct);
            await uow.SaveChangesAsync(ct);

            return Result<Guid>.Success(review.Id);
        }
    }
}
