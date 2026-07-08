using System;
using System.Threading.Tasks;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Abstract;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain.Invitations;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class InvitationService : IInvitationService
    {
        private readonly IUnitOfWork _uow;

        public InvitationService(IUnitOfWork uow)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<Invitation> CreateInvitationAsync(string email, int companyId, int invitedByUserId)
        {
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var inv = new Invitation
            {
                Email = email.Trim().ToLowerInvariant(),
                CompanyId = companyId,
                InvitedByUserId = invitedByUserId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                Accepted = false
            };

            await _uow.Invitations.AddAsync(inv);
            await _uow.SaveChangesAsync();
            return inv;
        }

        public async Task<bool> AcceptInvitationAsync(string token, int acceptingUserId)
        {
            var list = await _uow.Invitations.FindAsync(i => i.Token == token && !i.Accepted && i.ExpiresAt > DateTime.UtcNow);
            var inv = list.FirstOrDefault();
            if (inv == null) return false;


            inv.Accepted = true;
            inv.AcceptedAt = DateTime.UtcNow;
            inv.AcceptedByUserId = acceptingUserId;

            _uow.Invitations.Update(inv);
            await _uow.SaveChangesAsync();

            return true;
        }
    }
}
