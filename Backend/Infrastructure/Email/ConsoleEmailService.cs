using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Service.Interfaces;

namespace Infrastructure.Email
{
    public sealed class ConsoleEmailService(
     ILogger<ConsoleEmailService> logger,
     IOptions<FrontendSettings> options) : IEmailService
    {
        public Task SendEmailConfirmationAsync(
            string toEmail, string fullName, Guid userId, string token,
            CancellationToken cancellationToken = default)
        {
            var link = BuildLink("confirm-email", userId, token);
            logger.LogInformation("[FAKE EMAIL] Confirm email | To: {Email} | Name: {Name} | Link: {Link}", toEmail, fullName, link);
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(
            string toEmail, string fullName, Guid userId, string token,
            CancellationToken cancellationToken = default)
        {
            var link = BuildLink("reset-password", userId, token);
            logger.LogInformation("[FAKE EMAIL] Reset password | To: {Email} | Name: {Name} | Link: {Link}", toEmail, fullName, link);
            return Task.CompletedTask;
        }

        // DEV ONLY: replace with SMTP. Remove token logging before production.
        private string BuildLink(string path, Guid userId, string token)
            => $"{options.Value.BaseUrl.TrimEnd('/')}/{path}?userId={userId}&token={Uri.EscapeDataString(token)}";
    }

}
