using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Features.Profile.Commands.UpdateProfile;
using Service.Features.Profile.Queries.GetProfile;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/profile")]
    public class ProfileController(ISender sender) : ControllerBase
    {

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var result = await sender.Send(new GetProfileQuery(), ct);
            return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateProfileCommand command, CancellationToken ct)
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
        }
    }
}
