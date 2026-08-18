using System.ComponentModel.DataAnnotations;
using Onion.Domain;

namespace Onion.Domain.Credits
{
    public enum CreditStatus
    {
        PENDING = 0,
        ACTIVE = 1,
        PAID = 2,
        OVERDUE = 3,
        CANCELLED = 4
    }

    public class Credit : BaseEntity, Onion.Common.Models.ITenantEntity
    {
        public int CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance { get; set; }
        public DateTime DueDate { get; set; }
        public CreditStatus Status { get; set; } = CreditStatus.PENDING;
        public decimal? MinimumPaymentAmount { get; set; }
        public string? PaymentFrequency { get; set; }
        public int CompanyId { get; set; }

        // Navigation properties
        public ICollection<CreditPayment>? Payments { get; set; }
        public ICollection<CreditStatusHistory>? StatusHistory { get; set; }
    }
}
