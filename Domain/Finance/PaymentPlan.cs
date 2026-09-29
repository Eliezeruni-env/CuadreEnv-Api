using System;
using System.Collections.Generic;
using Onion.Domain;

namespace Onion.Domain.Finance
{
    // Represents a payment plan associated to an account receivable (installment schedule)
    public enum PaymentFrequency { Daily, Weekly, BiWeekly, Monthly }

    public class PaymentPlan : BaseEntity
    {
        public int AccountReceivableId { get; set; }
        public decimal InstallmentAmount { get; set; }
        public int TotalInstallments { get; set; }
        public PaymentFrequency Frequency { get; set; }
        public DateTime StartDate { get; set; }

        public List<Installment> Installments { get; set; } = new List<Installment>();
    }
}
