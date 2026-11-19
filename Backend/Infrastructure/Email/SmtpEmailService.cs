using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Service.Interfaces;

namespace Infrastructure.Email;

public sealed class SmtpEmailService(
    IOptions<SmtpSettings> smtpOptions,
    IOptions<FrontendSettings> frontendOptions,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    public Task SendEmailConfirmationAsync(
        string toEmail, string fullName, Guid userId, string token,
        CancellationToken cancellationToken = default)
    {
        var content = EmailTemplates.ConfirmEmail(fullName, BuildLink("confirm-email", userId, token));
        return SendAsync(toEmail, fullName, content, cancellationToken);
    }

    public Task SendPasswordResetAsync(
        string toEmail, string fullName, Guid userId, string token,
        CancellationToken cancellationToken = default)
    {
        var content = EmailTemplates.ResetPassword(fullName, BuildLink("reset-password", userId, token));
        return SendAsync(toEmail, fullName, content, cancellationToken);
    }

    // Never throws: a failure must not change the HTTP response (otherwise forgot-password
    // would answer differently for existing accounts and reveal who is registered).
    private async Task SendAsync(string toEmail, string toName, EmailContent content, CancellationToken ct)
    {
        var settings = smtpOptions.Value;

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(settings.FromName, settings.FromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = content.Subject;
            message.Body = new BodyBuilder { HtmlBody = content.Html, TextBody = content.Text }.ToMessageBody();

            // SmtpClient is not thread-safe: one instance per email
            using var client = new SmtpClient { Timeout = 15_000 };

            await client.ConnectAsync(settings.Host, settings.Port, SecureSocketOptions.Auto, ct);
            await client.AuthenticateAsync(settings.Username, settings.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation("Email '{Subject}' sent to {Recipient}", content.Subject, Mask(toEmail));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email '{Subject}' to {Recipient}", content.Subject, Mask(toEmail));
        }
    }

    private string BuildLink(string path, Guid userId, string token)
        => $"{frontendOptions.Value.BaseUrl.TrimEnd('/')}/{path}?userId={userId}&token={Uri.EscapeDataString(token)}";

    private static string Mask(string email)
    {
        var at = email.IndexOf('@');
        return at <= 1 ? "***" : $"{email[0]}***{email[at..]}";
    }
}