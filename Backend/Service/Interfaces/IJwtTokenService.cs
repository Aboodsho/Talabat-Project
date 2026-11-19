using Service.DTOs.Auth;

namespace Service.Interfaces
{
    public  interface IJwtTokenService
    {
        AccessTokenDto GenerateAccessToken(UserDto user);
    }
}

//  so from what i knew,
//  i pass userdto to jwttokenservices  because i want pass only values i need not all applicationuser and
//  make accesstokendto because the generateaccesstoken return token and its expiredate and
//  the authresponsedto we sill use it in handler to return it to user or angular because we want user info
