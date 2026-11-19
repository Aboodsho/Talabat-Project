using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Auth.Commands.ResetPassword
{
    public sealed class ResetPasswordCommandHandler(IIdentityService identityService)
     : IRequestHandler<ResetPasswordCommand, Result>
    {
        public Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
            => identityService.ResetPasswordAsync(request.UserId, request.Token, request.NewPassword, cancellationToken);
    }
}
