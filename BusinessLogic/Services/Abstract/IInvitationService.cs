using System.Threading.Tasks;
using Onion.Domain.Invitations;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IInvitationService
    {
        Task<Invitation> CreateInvitationAsync(string email, int companyId, int invitedByUserId);
        Task<bool> AcceptInvitationAsync(string token, int acceptingUserId);
    }
}
