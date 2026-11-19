using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Orders.Commands.Admin.AdminCancelOrder
{
    public sealed class AdminCancelOrderCommandHandler(IUnitOfWork uow) :
        IRequestHandler<AdminCancelOrderCommand, Result>

    {
        public async Task<Result> Handle(AdminCancelOrderCommand request, CancellationToken ct)
        {
            var order = await uow.Orders.GetByIdWithDetailsAsync(request.OrderId, ct);
            if (order is null)
                return Result.Failure("Order not found.");

            order.Cancel();
            await OrderStockRestorer.RestoreAsync(uow, order, ct);
            await uow.SaveChangesAsync(ct);

            return Result.Success();
        }
    }
}
