using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Features.Orders.Commands.CancelMyOrder;
using Service.Features.Orders.Commands.CheckOut;
using Service.Features.Orders.Queries.GetMyOrders;
using Service.Features.Orders.Queries.GetOrderById;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/orders")]
    public class OrdersController:ControllerBase
    {
        private readonly ISender _sender;

        public OrdersController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout(
           [FromBody] checkOutCommand command, CancellationToken ct)
        {
            var result = await _sender.Send(command, ct);

            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Value }, new { orderId = result.Value })
                : BadRequest(new { error = result.Error });
        }


        [HttpGet]
        public async Task<IActionResult> GetMyOrders(
            [FromQuery] GetMyOrdersQuery query, CancellationToken ct)
        {
            var result = await _sender.Send(query, ct);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var result = await _sender.Send(new GetOrderByIdQuery(id), ct);
            return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        {
            var result = await _sender.Send(new CancelMyOrderCommand(id), ct);
            return result.IsSuccess ? NoContent() : NotFound(result.Error);
        }
    }
}
