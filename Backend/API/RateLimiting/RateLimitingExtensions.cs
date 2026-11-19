using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.RateLimiting;

public static class RateLimitPolicies
{
    public const string Login = "auth-login";
    public const string Email = "auth-email";
    public const string Register = "auth-register";
}

public static class RateLimitingExtensions
{
    private static readonly TimeSpan GlobalWindow = TimeSpan.FromMinutes(1);

    // Single source of truth: used by both the policies and the Retry-After fallback
    private static readonly Dictionary<string, TimeSpan> PolicyWindows = new()
    {
        [RateLimitPolicies.Login] = TimeSpan.FromMinutes(1),
        [RateLimitPolicies.Email] = TimeSpan.FromMinutes(15),
        [RateLimitPolicies.Register] = TimeSpan.FromHours(1)
    };

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;

            // Applies to every request, on top of the named policies
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetSlidingWindowLimiter(ClientKey(ctx), _ => Sliding(100, GlobalWindow)));

            // Password guessing across many accounts
            options.AddPolicy(RateLimitPolicies.Login, ctx =>
                RateLimitPartition.GetSlidingWindowLimiter(ClientKey(ctx),
                    _ => Sliding(5, PolicyWindows[RateLimitPolicies.Login])));

            // Email spam (forgot-password, resend-confirmation)
            options.AddPolicy(RateLimitPolicies.Email, ctx =>
                RateLimitPartition.GetSlidingWindowLimiter(ClientKey(ctx),
                    _ => Sliding(3, PolicyWindows[RateLimitPolicies.Email])));

            // Fake account creation
            options.AddPolicy(RateLimitPolicies.Register, ctx =>
                RateLimitPartition.GetSlidingWindowLimiter(ClientKey(ctx),
                    _ => Sliding(5, PolicyWindows[RateLimitPolicies.Register])));
        });

        return services;
    }

    // Partition key = client IP. Behind a reverse proxy this needs ForwardedHeaders (deployment stage).
    private static string ClientKey(HttpContext ctx)
        => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static SlidingWindowRateLimiterOptions Sliding(int permitLimit, TimeSpan window) => new()
    {
        PermitLimit = permitLimit,
        Window = window,
        SegmentsPerWindow = 6,
        QueueLimit = 0,                 // reject immediately, never queue
        AutoReplenishment = true
    };

    private static TimeSpan ResolveRetryAfter(OnRejectedContext context)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            return retryAfter;

        // Sliding window gives no metadata: fall back to the policy window (an upper bound)
        var policy = context.HttpContext.GetEndpoint()?
            .Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;

        return policy is not null && PolicyWindows.TryGetValue(policy, out var window)
            ? window
            : GlobalWindow;
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken ct)
    {
        var http = context.HttpContext;

        http.Response.Headers.RetryAfter =
            Math.Ceiling(ResolveRetryAfter(context).TotalSeconds).ToString(CultureInfo.InvariantCulture);

        http.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimiting")
            .LogWarning("Rate limit exceeded: {Method} {Path} from {Ip}",
                http.Request.Method, http.Request.Path, ClientKey(http));

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests",
            Detail = "Too many requests. Please try again later.",
            Instance = http.Request.Path
        };
        problem.Extensions["code"] = "RATE_LIMITED";

        http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await http.Response.WriteAsJsonAsync(problem, ct);
    }
}