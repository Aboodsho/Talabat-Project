using Infrastructure.Data;
using Infrastructure.Email;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Service.Interfaces;





namespace Infrastructure
{
    public  static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

            services.AddDataProtection();

            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

            })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.AddScoped<IUnitOfWork, Infrastructure.UnitOfWork.UnitOfWork>();
            services.AddScoped<IIdentityService, IdentityService>();

            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
            services.AddSingleton<IJwtTokenService, JwtTokenService>();

            services.AddScoped<IUserLookupService, UserLookupService>();

            services.AddScoped<IProfileService, ProfileService>();


            services.Configure<FrontendSettings>(configuration.GetSection(FrontendSettings.SectionName));

            var emailProvider = configuration["Email:Provider"] ?? "Console";

            if (emailProvider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
            {
                services.AddOptions<SmtpSettings>()
                    .Bind(configuration.GetSection(SmtpSettings.SectionName))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();                       // fail at startup if a required value is missing

                services.AddScoped<IEmailService, SmtpEmailService>();
            }
            else if (emailProvider.Equals("Console", StringComparison.OrdinalIgnoreCase))
            {
                services.AddScoped<IEmailService, ConsoleEmailService>();
            }
            else
            {
                throw new InvalidOperationException($"Unknown Email:Provider '{emailProvider}'. Use 'Console' or 'Smtp'.");
            }
            services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(24));

            return services;
        }

    }
}
