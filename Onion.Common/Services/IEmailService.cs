using System.Threading.Tasks;

namespace Onion.Common.Services
{
    public interface IEmailService
    {
        Task SendInvitationAsync(string email, string token, int companyId);
    }
}
