using Core.Results;
using MediatR;
using Service.DTOs.Auth;

namespace Service.Features.Auth.Commands.Register
{
    public record RegisterCommand(string Email, string Password, string FullName):IRequest<Result<UserDto>>;
   

}
