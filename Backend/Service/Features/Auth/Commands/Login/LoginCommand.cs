using Core.Results;
using MediatR;
using Service.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Auth.Commands.Login
{
    public sealed record LoginCommand(string Email, string Password):IRequest<Result<AuthResponseDto>>;
    
}
