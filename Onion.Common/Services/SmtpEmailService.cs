using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Onion.Common.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly string? _host;
        private readonly int _port;
        private readonly string? _user;
        private readonly string? _pass;

        public SmtpEmailService()
        {
            // Read configuration from environment variables as a simple approach
            _host = Environment.GetEnvironmentVariable("SMTP_HOST");
            _port = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var p) ? p : 25;
            _user = Environment.GetEnvironmentVariable("SMTP_USER");
            _pass = Environment.GetEnvironmentVariable("SMTP_PASS");
        }
        public Task SendInvitationAsync(string email, string token, int companyId)
        {
            if (string.IsNullOrWhiteSpace(_host))
            {
                // No SMTP configured; do nothing but return completed task
                return Task.CompletedTask;
            }

            try
            {
                using var client = new SmtpClient(_host, _port)
                {
                    EnableSsl = true
                };

                if (!string.IsNullOrWhiteSpace(_user))
                {
                    client.Credentials = new NetworkCredential(_user, _pass ?? string.Empty);
                }

                var msg = new MailMessage("no-reply@onion.local", email)
                {
                    Subject = "You are invited",
                    Body = $"You were invited to join company {companyId}. Use token: {token} to accept the invitation."
                };

                client.Send(msg);
            }
            catch
            {
                // Swallow exceptions; emailing is optional. In production log properly.
            }

            return Task.CompletedTask;
        }

        public Task SendEmailAsync(string to, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(_host))
            {
                return Task.CompletedTask;
            }

            try
            {
                using var client = new SmtpClient(_host, _port)
                {
                    EnableSsl = true
                };

                if (!string.IsNullOrWhiteSpace(_user))
                {
                    client.Credentials = new NetworkCredential(_user, _pass ?? string.Empty);
                }

                var msg = new MailMessage("no-reply@onion.local", to)
                {
                    Subject = subject,
                    Body = body
                };

                client.Send(msg);
            }
            catch
            {
                // swallow in this minimal implementation
            }

            return Task.CompletedTask;
        }
    }
}
