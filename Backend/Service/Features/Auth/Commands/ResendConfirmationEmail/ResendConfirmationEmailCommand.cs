using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Auth.Commands.ResendConfirmationEmail
{
    public sealed record ResendConfirmationEmailCommand(string Email) : IRequest<Result>;

}
