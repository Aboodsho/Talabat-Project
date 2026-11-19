using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Auth.Commands.ConfirmEmail
{
    public sealed record ConfirmEmailCommand(Guid UserId, string Token) : IRequest<Result>;

}
