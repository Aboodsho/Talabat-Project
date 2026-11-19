using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Features.Reviews.Commands.AddReview;
using Service.Features.Reviews.Commands.DeleteMyReview;
using Service.Features.Reviews.Commands.UpdateMyReview;
using Service.Features.Reviews.Queries;

namespace API.Controllers
{
    [ApiController]
    [Route("api/products/{productId:guid}/reviews")]
    public sealed  class ReviewsController(IMediator mediator) : ControllerBase
    {

        [HttpGet]
        public async Task<IActionResult> GetAll(Guid productId, [FromQuery] GetProductReviewsQuery query, CancellationToken ct)
        {
            query.ProductId = productId;
            var result = await mediator.Send(query, ct);
            return result.IsSuccess ? Ok(result.Value): NotFound(result.Error);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Add(Guid productId, [FromBody] AddReviewCommand command, CancellationToken ct)
        {
            command.ProductId = productId;
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetAll), new { productId }, new { id = result.Value })
                : NotFound(result.Error);
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> Update(Guid productId, [FromBody] UpdateMyReviewCommand command, CancellationToken ct)
        {
            command.ProductId = productId;
            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? NoContent() : NotFound(result.Error);
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid productId, CancellationToken ct)
        {
            var result = await mediator.Send(new DeleteMyReviewCommand(productId), ct);
            return result.IsSuccess ? NoContent() : NotFound(result.Error);
        }

    }
}
