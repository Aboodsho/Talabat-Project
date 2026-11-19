using Core.Results;
using MediatR;
using Service.DTOs.Auth;
using Service.Interfaces;

namespace Service.Features.Auth.Commands.Register
{
    public class RegisterCommandHandler(IIdentityService identityService, IEmailService emailService)
        : IRequestHandler<RegisterCommand, Result<UserDto>>
    {
        public async Task<Result<UserDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var email = request.Email.Trim();

            var result = await identityService.RegisterAsync(
                email, request.Password, request.FullName.Trim(), cancellationToken);

            if (!result.IsSuccess)
                return result;

            var info = await identityService.CreateEmailConfirmationTokenAsync(email, cancellationToken);
            if (info is not null)
                await emailService.SendEmailConfirmationAsync(
                    info.Email, info.FullName, info.UserId, info.Token, cancellationToken);

            return result;
        }
    }
}
