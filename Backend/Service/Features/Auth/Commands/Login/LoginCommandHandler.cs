using Core.Results;
using MediatR;
using Service.DTOs.Auth;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Auth.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
    {
        private readonly IIdentityService _identityService;
        private readonly IJwtTokenService _jwtTokenService;

        public LoginCommandHandler(IIdentityService identityService, IJwtTokenService jwtTokenService)
        {
            _identityService = identityService;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var loginResult = await _identityService.LoginAsync(request.Email.Trim(),request.Password);
            if (!loginResult.IsSuccess)
                return Result<AuthResponseDto>.Failure(loginResult.Error!);
            var user = loginResult.Value!;
            var token=_jwtTokenService.GenerateAccessToken(user);

            return Result<AuthResponseDto>.Success(new AuthResponseDto(token.Token, token.ExpireAtUtc, user));


        }
    }
}
