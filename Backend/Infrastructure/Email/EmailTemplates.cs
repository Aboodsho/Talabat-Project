using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Email
{
    internal sealed record EmailContent(string Subject, string Html, string Text);

    internal static class EmailTemplates
    {
        public static EmailContent ConfirmEmail(string fullName, string link) => Build(
            subject: "Confirm your email",
            heading: "Welcome!",
            intro: "Please confirm your email address to activate your account.",
            buttonText: "Confirm email",
            footer: "This link expires in 24 hours. If you did not create an account, ignore this email.",
            fullName, link);

        public static EmailContent ResetPassword(string fullName, string link) => Build(
            subject: "Reset your password",
            heading: "Password reset",
            intro: "We received a request to reset your password.",
            buttonText: "Reset password",
            footer: "This link expires in 24 hours. If you did not request this, ignore this email and your password stays the same.",
            fullName, link);

        private static EmailContent Build(
            string subject, string heading, string intro, string buttonText, string footer,
            string fullName, string link)
        {
            // fullName is user input: always HTML-encode it before putting it in HTML
            var name = WebHtml(fullName);
            var href = WebHtml(link);

            var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto;padding:24px;color:#222">
              <h2 style="margin:0 0 16px">{heading}</h2>
              <p>Hi {name},</p>
              <p>{intro}</p>
              <p style="margin:24px 0">
                <a href="{href}" style="background:#2563eb;color:#fff;padding:12px 20px;border-radius:6px;text-decoration:none;display:inline-block">{buttonText}</a>
              </p>
              <p style="font-size:13px;color:#555">If the button does not work, copy this link into your browser:<br>{href}</p>
              <p style="font-size:12px;color:#888">{footer}</p>
            </div>
            """;

            var text = $"Hi {fullName},\n\n{intro}\n\n{link}\n\n{footer}";

            return new EmailContent(subject, html, text);
        }

        private static string WebHtml(string value) => WebUtility.HtmlEncode(value);
    }
}
