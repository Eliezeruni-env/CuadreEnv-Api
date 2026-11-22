using System;

namespace Onion.Domain.Finance
{
    public class BankReconciliationRecord : BaseEntity
    {
        public int CompanyId { get; set; }
        public int? SaleId { get; set; }
        public string BankReference { get; set; } = string.Empty; // e.g. TRF-00083421
        public string InvoiceNumber { get; set; } = string.Empty; // e.g. FAC-0158
        public string CustomerName { get; set; } = string.Empty; // e.g. Comercial Norte, S.R.L.
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public string DestinationBank { get; set; } = "Banco Popular"; // Banco Popular, Banco BHD, Banco Santa Cruz
        public string? AccountLastFour { get; set; }
        public decimal RegisteredAmount { get; set; }
        public decimal? StatementAmount { get; set; }
        public decimal DifferenceAmount { get; set; }
        public string Status { get; set; } = "PENDING"; // PENDING, VERIFIED, DISCREPANT
        public string? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? DiscrepancyReason { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
