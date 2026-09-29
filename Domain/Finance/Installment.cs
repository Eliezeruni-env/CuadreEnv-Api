using System;
using Onion.Domain;

namespace Onion.Domain.Finance
{
    public enum InstallmentStatus { Expected, Partial, Paid, Overdue }

    public class Installment : BaseEntity
    {
        public int PaymentPlanId { get; set; }
        public int Number { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public InstallmentStatus Status { get; set; }

        public decimal Balance => Amount - PaidAmount;
    }
}
