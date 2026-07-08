using System;
using Onion.Domain.Users;

namespace Onion.Domain.Invitations
{
    public class Invitation : Onion.Domain.BaseEntity
    {
        public string Email { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public int InvitedByUserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool Accepted { get; set; }
        public int? AcceptedByUserId { get; set; }
        public DateTime? AcceptedAt { get; set; }
    }
}
