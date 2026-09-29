using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Onion.Common.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public Task SendInvitationAsync(string email, string token, int companyId)
        {
            return SendEmailAsync(
                email,
                "You are invited",
                $"You were invited to join company {companyId}. Use token: {token} to accept the invitation.",
                isHtml: false);
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, byte[]? attachmentBytes = null, string? attachmentName = null)
        {
            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentException("Recipient email is required.", nameof(to));

            var smtp = _config.GetSection("Smtp");
            var host = GetValue(smtp["Host"], "Smtp__Host", "SMTP__HOST", "SMTP_HOST");
            var user = GetValue(smtp["User"], "Smtp__User", "SMTP__USER", "SMTP_USER");
            var password = GetValue(smtp["Password"], "Smtp__Password", "SMTP__PASSWORD", "SMTP_PASSWORD", "SMTP_PASS");
            var fromEmail = GetValue(smtp["FromEmail"], "Smtp__FromEmail", "SMTP__FROMEMAIL", "SMTP_FROM_EMAIL") ?? user;
            var fromName = GetValue(smtp["FromName"], "Smtp__FromName", "SMTP__FROMNAME", "SMTP_FROM_NAME") ?? "CuadreEnv";
            var port = GetIntValue(smtp["Port"], "Smtp__Port", "SMTP__PORT", 587);
            var enableSsl = GetBoolValue(smtp["EnableSsl"], "Smtp__EnableSsl", "SMTP__ENABLESSL", "SMTP_ENABLE_SSL", true);

            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("SMTP is not configured. Set Smtp:Host or SMTP_HOST.");
            if (port is < 1 or > 65535)
                throw new InvalidOperationException("SMTP port is invalid. Set Smtp:Port to a value between 1 and 65535.");
            if (string.IsNullOrWhiteSpace(fromEmail))
                throw new InvalidOperationException("SMTP sender is not configured. Set Smtp:FromEmail or SMTP_FROM_EMAIL.");
            if (!MailAddress.TryCreate(fromEmail, out _))
                throw new InvalidOperationException("SMTP sender email is invalid.");
            if (!string.IsNullOrWhiteSpace(user) && string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("SMTP password is missing. Configure Smtp:Password or SMTP_PASSWORD.");
            if (string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("SMTP user is missing. Configure Smtp:User or SMTP_USER.");

            try
            {
                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Timeout = 15000
                };

                if (!string.IsNullOrWhiteSpace(user))
                {
                    var cleanPassword = (password ?? string.Empty).Replace(" ", "").Trim();
                    client.Credentials = new NetworkCredential(user.Trim(), cleanPassword);
                }

                using var msg = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };
                msg.To.Add(new MailAddress(to));

                if (attachmentBytes is { Length: > 0 })
                {
                    if (string.IsNullOrWhiteSpace(attachmentName))
                        throw new ArgumentException("Attachment name is required when attachment bytes are supplied.", nameof(attachmentName));
                    msg.Attachments.Add(new Attachment(new System.IO.MemoryStream(attachmentBytes), attachmentName));
                }

                await client.SendMailAsync(msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP email delivery failed for recipient {Recipient} with subject {Subject}.", to, subject);
                throw;
            }
        }

        private string? GetValue(string? configurationValue, params string[] environmentNames)
        {
            if (!string.IsNullOrWhiteSpace(configurationValue))
                return configurationValue;

            foreach (var name in environmentNames)
            {
                var value = Environment.GetEnvironmentVariable(name);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }

        private int GetIntValue(string? configurationValue, string environmentName, string legacyEnvironmentName, int fallback)
        {
            var value = GetValue(configurationValue, environmentName, legacyEnvironmentName);
            return int.TryParse(value, out var parsed) ? parsed : fallback;
        }

        private bool GetBoolValue(string? configurationValue, string environmentName, string legacyEnvironmentName, string legacyEnvironmentName2, bool fallback)
        {
            var value = GetValue(configurationValue, environmentName, legacyEnvironmentName, legacyEnvironmentName2);
            return bool.TryParse(value, out var parsed) ? parsed : fallback;
        }
    }
}
