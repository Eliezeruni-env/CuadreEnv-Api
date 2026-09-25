using System.Collections.Generic;
using System.Threading.Tasks;
using Onion.Common.Services;

namespace Onion.IntegrationTests
{
    // Test-only email service that records sent emails for assertions.
    public class TestEmailService : IEmailService
    {
        public List<(string To, string Subject, string Body)> Sent { get; } = new List<(string, string, string)>();

        public Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, byte[]? attachmentBytes = null, string? attachmentName = null)
        {
            Sent.Add((to, subject, body));
            return Task.CompletedTask;
        }

        public Task SendInvitationAsync(string email, string token, int companyId)
        {
            Sent.Add((email, "Invitation", $"Token:{token};Company:{companyId}"));
            return Task.CompletedTask;
        }
    }
}
