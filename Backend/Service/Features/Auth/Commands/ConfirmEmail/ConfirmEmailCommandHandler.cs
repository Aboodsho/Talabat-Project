using Core.Results;
using MediatR;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Auth.Commands.ConfirmEmail
{
    public sealed class ConfirmEmailCommandHandler(IIdentityService identityService)
    : IRequestHandler<ConfirmEmailCommand, Result>
    {
        public Task<Result> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
            => identityService.ConfirmEmailAsync(request.UserId, request.Token, cancellationToken);
    }
}
