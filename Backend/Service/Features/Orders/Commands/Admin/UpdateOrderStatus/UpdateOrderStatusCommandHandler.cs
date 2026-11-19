using Core.Results;
using MediatR;
using Service.Interfaces;

namespace Service.Features.Orders.Commands.Admin.UpdateOrderStatus
{
    public sealed class UpdateOrderStatusCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateOrderStatusCommand, Result>
    {
        public async Task<Result> Handle(UpdateOrderStatusCommand request, CancellationToken ct)
        {
            var order = await uow.Orders.GetByIdWithDetailsAsync(request.OrderId, ct);
            if (order is null)
                return Result.Failure("Order not found.");

            switch (request.Action)
            {
                case OrderAction.Confirm: order.Confirm(); break;
                case OrderAction.Ship: order.Ship(); break;
                case OrderAction.Deliver: order.Deliver(); break;
            }

            await uow.SaveChangesAsync(ct);
            return Result.Success();
        }
    }
}
