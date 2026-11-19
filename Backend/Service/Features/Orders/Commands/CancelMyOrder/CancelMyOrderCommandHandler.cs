using Core.Results;
using MediatR;
using Service.Interfaces;

namespace Service.Features.Orders.Commands.CancelMyOrder
{
    public sealed class CancelMyOrderCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
        : IRequestHandler<CancelMyOrderCommand, Result>
    {
        public async Task<Result> Handle(CancelMyOrderCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId?? throw new UnauthorizedAccessException();

            var order = await uow.Orders.GetByIdWithDetailsAsync(request.OrderId, cancellationToken);
            if (order is null || order.CustomerId != userId)
                return Result.Failure("Order not found.");
            order.Cancel();
            await OrderStockRestorer.RestoreAsync(uow, order, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            return Result.Success();



        }
    }
}
