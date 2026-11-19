using Core.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Features.Wishlists.Commands.AddItem;
using Service.Features.Wishlists.Commands.Clear;
using Service.Features.Wishlists.Commands.Move_to_cart;
using Service.Features.Wishlists.Commands.Remove_item;
using Service.Features.Wishlists.Queries.Get_wishlist;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/wishlist")]
    public class WishlistController : ControllerBase
    {
        private readonly ISender _sender;
        public WishlistController(ISender sender)
        {
            _sender = sender;
        }

        private IActionResult ToResponse(Result result)
            => result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });

        [HttpGet]
        public async Task<IActionResult> GetWishlist(CancellationToken ct)
        {
            var Result = await _sender.Send(new GetWishlistQuery(), ct);
            return Result.IsSuccess ? Ok(Result.Value) : BadRequest(new { error = Result.Error });
        }

        [HttpPost("items/{productId:guid}")]
        public async Task<IActionResult> AddItem(Guid productId, CancellationToken ct)
            => ToResponse(await _sender.Send(new AddWishlistItemCommand(productId), ct));

        [HttpPost("items/{productId:guid}/Move-To-Cart")]
        public async Task<IActionResult> MoveToCart(Guid productId, CancellationToken ct)
            => ToResponse(await _sender.Send(new MoveWishlistItemToCartCommand(productId), ct));

        [HttpDelete("items/{productId:guid}")]
        public async Task<IActionResult> RemoveItem(Guid productId, CancellationToken ct)
           => ToResponse(await _sender.Send(new RemoveWishlistItemCommand(productId), ct));

        [HttpDelete("clear")]
        public async Task<IActionResult> Clear(CancellationToken ct)
           => ToResponse(await _sender.Send(new ClearWishlistCommand(), ct));

    }
}
