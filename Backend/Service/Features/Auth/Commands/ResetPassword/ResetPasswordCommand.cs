using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Auth.Commands.ResetPassword
{
    public sealed record ResetPasswordCommand(Guid UserId, string Token, string NewPassword) : IRequest<Result>;

}
