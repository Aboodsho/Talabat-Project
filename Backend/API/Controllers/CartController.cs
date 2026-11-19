using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Features.Cart.Commands.AddItemToCart;
using Service.Features.Cart.Commands.Clear_cart;
using Service.Features.Cart.Commands.DecreaseCartItem;
using Service.Features.Cart.Commands.RemoveCartItem;
using Service.Features.Cart.Queries.GetCart;


namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/cart")]

    public class CartController: ControllerBase
    {
        private readonly ISender _sender;

        public CartController(ISender sender)
        {
            _sender = sender;
        }

        private IActionResult ToResponse(Result result)
            => result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });

        [HttpGet]
        public async Task<IActionResult> GetCart(CancellationToken ct)
        {
            var result= await _sender.Send(new GetCartQuery(), ct);
            return result.IsSuccess? Ok(result.Value) : BadRequest(new { error = result.Error });
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem(
           [FromBody] AddItemToCartCommand command, CancellationToken ct)
           => ToResponse(await _sender.Send(command, ct));

        [HttpPatch("items/{productId:guid}/decrease")]
        public async Task<IActionResult> DecreaseItem(
            Guid productId, [FromBody] DecreaseCartItemCommand command, CancellationToken ct)
        {
            command.ProductId = productId;
            return ToResponse(await _sender.Send(command, ct));
        }

        [HttpDelete("items/{productId:guid}")]
        public async Task<IActionResult> RemoveItem(Guid productId, CancellationToken ct)
           => ToResponse(await _sender.Send(new RemoveCartItemCommand(productId), ct));

        [HttpDelete("Clear")]
        public async Task<IActionResult> Clear(CancellationToken ct)
            => ToResponse(await _sender.Send(new ClearCartCommand(), ct));

    }
}
