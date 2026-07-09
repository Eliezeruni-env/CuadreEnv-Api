using System.Threading.Tasks;

namespace Onion.Common.Services
{
    public interface IEmailService
    {
        Task SendInvitationAsync(string email, string token, int companyId);
        Task SendEmailAsync(string to, string subject, string body);
    }
}
