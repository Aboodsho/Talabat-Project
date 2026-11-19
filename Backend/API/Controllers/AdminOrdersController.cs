using Core.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Service.Features.Orders.Commands.Admin.AdminCancelOrder;
using Service.Features.Orders.Commands.Admin.UpdateOrderStatus;
using Service.Features.Orders.Queries.Admin.GetAllOrders;
using Service.Features.Orders.Queries.Admin.GetOrderById;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/orders")]
    [Authorize(Roles = Role.Admin)]
    public class AdminOrdersController(ISender _sender):ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllOrdersQuery query, CancellationToken ct)
        => Ok(await _sender.Send(query, ct));


        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var result = await _sender.Send(new GetOrderByIdQuery(id), ct);
            return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
        }

        private async Task<IActionResult> Update(Guid id, OrderAction action, CancellationToken ct)
        {
            var result = await _sender.Send(new UpdateOrderStatusCommand(id, action), ct);
            return result.IsSuccess ? NoContent() : NotFound(result.Error);
        }

        [HttpPost("{id:guid}/confirm")]
        public Task<IActionResult> Confirm(Guid id, CancellationToken ct)
            => Update(id, OrderAction.Confirm, ct);


        [HttpPost("{id:guid}/ship")]
        public Task<IActionResult> Ship(Guid id, CancellationToken ct)
            => Update(id, OrderAction.Ship, ct);


        [HttpPost("{id:guid}/deliver")]
        public Task<IActionResult> Deliver(Guid id, CancellationToken ct)
            => Update(id, OrderAction.Deliver, ct);


        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        {
            var result = await _sender.Send(new AdminCancelOrderCommand(id), ct);
            return result.IsSuccess ? NoContent() : NotFound(result.Error);
        }





    }
}
