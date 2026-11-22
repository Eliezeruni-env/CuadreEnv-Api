using System;
using System.Collections.Generic;

namespace Onion.BussinesLogic.Dtos
{
    public class BankTransferRecordDto
    {
        public int Id { get; set; }
        public int? SaleId { get; set; }
        public string BankReference { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string DestinationBank { get; set; } = string.Empty;
        public string? AccountLastFour { get; set; }
        public decimal RegisteredAmount { get; set; }
        public decimal? StatementAmount { get; set; }
        public decimal DifferenceAmount { get; set; }
        public string Status { get; set; } = "PENDING"; // PENDING, VERIFIED, DISCREPANT
        public string? VerifiedBy { get; set; }
        public string? VerifiedAt { get; set; }
        public string? DiscrepancyReason { get; set; }
        public string? Notes { get; set; }
    }

    public class BankReconciliationStatsDto
    {
        public decimal TotalTransfers { get; set; }
        public int TotalTransfersCount { get; set; }
        public decimal ReconciledAmount { get; set; }
        public int ReconciledCount { get; set; }
        public decimal PendingAmount { get; set; }
        public int PendingCount { get; set; }
        public decimal DiscrepantAmount { get; set; }
        public int DiscrepantCount { get; set; }
    }

    public class ReconcileTransferRequest
    {
        public decimal? StatementAmount { get; set; }
        public string? VerifiedBy { get; set; }
        public string? Notes { get; set; }
    }

    public class MarkDiscrepancyRequest
    {
        public decimal StatementAmount { get; set; }
        public decimal DifferenceAmount { get; set; }
        public string DiscrepancyReason { get; set; } = string.Empty;
        public string? VerifiedBy { get; set; }
    }

    public class ImportStatementItem
    {
        public string BankReference { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string? DestinationBank { get; set; }
    }

    public class ImportStatementRequest
    {
        public string DestinationBank { get; set; } = "Banco Popular";
        public List<ImportStatementItem> Items { get; set; } = new List<ImportStatementItem>();
    }
}
