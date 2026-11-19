using Core.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Features.Reviews.Commands.AdminDeleteReview;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/reviews")]
    [Authorize(Roles = Role.Admin)]
    public class AdminReviewsController(IMediator mediator) : ControllerBase
    {
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var result = await mediator.Send(new AdminDeleteReviewCommand(id), ct);
            return result.IsSuccess ? NoContent() : NotFound(result.Error);
        }
    }
}
