using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Auth.Commands.ForgotPassword
{
    public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;

}
