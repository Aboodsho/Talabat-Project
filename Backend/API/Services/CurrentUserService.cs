
using System.IdentityModel.Tokens.Jwt;

namespace API.Services
{
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?
                    .User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                return Guid.TryParse(value, out var id) ? id : null;
            }
        }
        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    }
}
