
using API.RateLimiting;
using Infrastructure;
using Infrastructure.Identity;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Service;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

namespace API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Serilog
            builder.Host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext());

            // Add services to the container.

            builder.Services.AddControllers()
                .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddEndpointsApiExplorer();
            
            // authorize for swagger
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Paste the JWT token only (no 'Bearer ' prefix)."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                     Array.Empty<string>()
        }
    });
            });

            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddService();

            //ICurrentUserService
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, API.Services.CurrentUserService>();

            var jwt = builder.Configuration
                            .GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwt.SecretKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        RoleClaimType = ClaimTypes.Role
                    };
                });

            builder.Services.AddAuthorization();
            // exception handler
            builder.Services.AddExceptionHandler<API.Middleware.GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            const string CorsPolicy = "Frontend";

            builder.Services.AddCors(options =>
                options.AddPolicy(CorsPolicy, policy => policy
                    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
                    .AllowAnyHeader()
                    .AllowAnyMethod()));

            // with the other services (before builder.Build())
            builder.Services.AddApiRateLimiting();


            var app = builder.Build();

            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate =
                    "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms by {UserId}";

                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    // MapInboundClaims = false, so the claim type is "sub"
                    var userId = httpContext.User.FindFirst("sub")?.Value ?? "anonymous";
                    diagnosticContext.Set("UserId", userId);
                };
            });

            // exception handler [IExceptionHandler]
            app.UseExceptionHandler();

            // pipeline: right AFTER UseCors, so the 429 response keeps the CORS headers
            app.UseCors(CorsPolicy);
            app.UseRateLimiter();

            // seeding role and admin
            await IdentitySeeder.SeedRoleAsync(app.Services);
            await IdentitySeeder.SeedAdminAsync(app.Services, app.Configuration);
            await StoreCatalogSeeder.SeedAsync(app.Services);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
               
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}

