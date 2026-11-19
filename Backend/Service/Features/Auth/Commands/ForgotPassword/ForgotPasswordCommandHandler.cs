using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Auth.Commands.ForgotPassword
{
    public sealed class ForgotPasswordCommandHandler(IIdentityService identityService, IEmailService emailService)
    : IRequestHandler<ForgotPasswordCommand, Result>
    {
        public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var info = await identityService.CreatePasswordResetTokenAsync(request.Email.Trim(), cancellationToken);

            if (info is not null)
                await emailService.SendPasswordResetAsync(
                    info.Email, info.FullName, info.UserId, info.Token, cancellationToken);

            return Result.Success();      // ALWAYS success: never reveal if the email exists
        }
    }
}
