using Onion.Domain;

namespace Onion.Domain.Credits
{
    public class CreditStatusHistory : BaseEntity, Onion.Common.Models.ITenantEntity
    {
        public int CreditId { get; set; }
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public int ChangedByUserId { get; set; }
        public int CompanyId { get; set; }

        // Navigation
        public Credit? Credit { get; set; }
    }
}
