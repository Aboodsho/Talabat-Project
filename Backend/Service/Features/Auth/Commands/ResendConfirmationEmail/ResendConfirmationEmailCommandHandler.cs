using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Auth.Commands.ResendConfirmationEmail
{
    public sealed class ResendConfirmationEmailCommandHandler(IIdentityService identityService, IEmailService emailService)
    : IRequestHandler<ResendConfirmationEmailCommand, Result>
    {
        public async Task<Result> Handle(ResendConfirmationEmailCommand request, CancellationToken cancellationToken)
        {
            var info = await identityService.CreateEmailConfirmationTokenAsync(request.Email.Trim(), cancellationToken);

            if (info is not null)
                await emailService.SendEmailConfirmationAsync(
                    info.Email, info.FullName, info.UserId, info.Token, cancellationToken);

            return Result.Success();      // ALWAYS success: never reveal if the email exists
        }
    }
}
