using API.RateLimiting;
using Core.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Service.Features.Auth.Commands.ConfirmEmail;
using Service.Features.Auth.Commands.ForgotPassword;
using Service.Features.Auth.Commands.Login;
using Service.Features.Auth.Commands.Register;
using Service.Features.Auth.Commands.ResendConfirmationEmail;
using Service.Features.Auth.Commands.ResetPassword;

namespace API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController:ControllerBase
    {
        private readonly ISender _sender;

        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        [EnableRateLimiting(RateLimitPolicies.Register)]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken )
        {
            var result = await _sender.Send(command, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
        }

        [EnableRateLimiting(RateLimitPolicies.Login)]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken) 
        {
            var result = await _sender.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return result.Error == AuthErrors.EmailNotConfirmed
                    ? StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error })
                    : Unauthorized(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me([FromServices] ICurrentUserService currentUser)
                 => Ok(new { currentUser.UserId, currentUser.IsAuthenticated });


        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command, CancellationToken ct)
        {
            var result = await _sender.Send(command, ct);          // use your injected sender name
            return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
        }


        [EnableRateLimiting(RateLimitPolicies.Email)]
        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationEmailCommand command, CancellationToken ct)
        {
            await _sender.Send(command, ct);
            return Ok(new { message = "If the account exists and is not confirmed, a new email was sent." });
        }


        [EnableRateLimiting(RateLimitPolicies.Email)]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken ct)
        {
            await _sender.Send(command, ct);          // use your injected sender name
            return Ok(new { message = "If the account exists, a password reset email was sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken ct)
        {
            var result = await _sender.Send(command, ct);
            return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
        }




    }
}
