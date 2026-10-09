using System.Threading.Tasks;
using Onion.Common.Services;
using Microsoft.Extensions.Configuration;
namespace Onion.BusinessLogic.Services.Concrete
{
    // Minimal SMTP/email service skeleton. Implement proper SMTP using IConfiguration and a mail client as needed.
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public SmtpEmailService(IConfiguration config)
        {
            _config = config;
        }

        public Task SendInvitationAsync(string email, string token, int companyId)
        {
            // TODO: Implement real invitation email sending using SMTP or external provider.
            return Task.CompletedTask;
        }

        public Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, byte[]? attachmentBytes = null, string? attachmentName = null)
        {
            // TODO: Implement real send
            return Task.CompletedTask;
        }
    }
}
