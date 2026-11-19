using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Identity
{
    public sealed class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public string SecretKey { get; init; } = string.Empty;
        public int AccessTokenMinutes { get; init; } = 30;

    }
}

// i access JwtSettings to appsettings.json by write in addinfrastructure (dependenceinjection file in infra)
// services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
