namespace Service.DTOs.Auth
{
    public sealed record AccessTokenDto(string Token , DateTime ExpireAtUtc);
   
}
