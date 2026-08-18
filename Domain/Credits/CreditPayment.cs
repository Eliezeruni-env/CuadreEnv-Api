using Onion.Domain;

namespace Onion.Domain.Credits
{
    public class CreditPayment : BaseEntity, Onion.Common.Models.ITenantEntity
    {
        public int CreditId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public string? Notes { get; set; }
        public int CompanyId { get; set; }

        // Navigation
        public Credit? Credit { get; set; }
    }
}
