using Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;              
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace API.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            int status;
            string title;
            string details;
            string? code = null;

            switch (exception)
            {
                case ValidationException ex:
                    status = StatusCodes.Status400BadRequest;
                    title = "Validation failed";
                    details = ex.Message;
                    code = "VALIDATION_ERROR";
                    _logger.LogWarning("Validation failed: {Message}", ex.Message);
                    break;

                case DomainException ex:
                    status = StatusCodes.Status400BadRequest;
                    title = "Business rule violated";
                    details = ex.Message;
                    code = ex.Code;
                    _logger.LogWarning("Domain rule violated [{Code}]: {Message}", ex.Code, ex.Message);
                    break;

                // MUST stay before the general DbUpdateException case below
                case DbUpdateConcurrencyException ex:
                    status = StatusCodes.Status409Conflict;
                    title = "Conflict";
                    details = "The data was changed by another request. Please retry.";
                    code = "CONCURRENCY_CONFLICT";
                    _logger.LogWarning(ex, "Concurrency conflict");
                    break;

                // NEW: SQL Server 2601 = duplicate in unique index, 2627 = unique constraint violation
                case DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } ex:
                    status = StatusCodes.Status409Conflict;
                    title = "Conflict";
                    details = "A record with the same unique value already exists.";
                    code = "DUPLICATE_RESOURCE";
                    _logger.LogWarning(ex, "Unique constraint violation");
                    break;

                default:
                    status = StatusCodes.Status500InternalServerError;
                    title = "Server error";
                    details = "An unexpected error occurred.";
                    _logger.LogError(exception, "Unhandled exception for user {UserId}",
                        httpContext.User.FindFirst("sub")?.Value ?? "anonymous");     // NEW (optional)
                    break;
            }

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = details,
                Instance = httpContext.Request.Path,
            };
            if (code is not null)
            {
                problem.Extensions["code"] = code;
            }

            // ===== NEW: re-apply CORS headers (the exception middleware clears them) =====
            var corsService = httpContext.RequestServices.GetRequiredService<ICorsService>();
            var corsProvider = httpContext.RequestServices.GetRequiredService<ICorsPolicyProvider>();
            var corsPolicy = await corsProvider.GetPolicyAsync(httpContext, "Frontend");

            if (corsPolicy is not null)
            {
                var corsResult = corsService.EvaluatePolicy(httpContext, corsPolicy);
                corsService.ApplyResult(corsResult, httpContext.Response);
            }
            // ==============================================================================

            httpContext.Response.StatusCode = status;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }
    }
}