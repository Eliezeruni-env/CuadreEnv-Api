using Onion.Domain;

namespace Onion.Domain.Invoices;

public enum FiscalDocumentStatus
{
    Pending,
    Processing,
    Submitted,
    Accepted,
    Rejected,
    Retry
}

public class FiscalDocument : BaseEntity
{
    public int CompanyId { get; set; }
    public int SaleId { get; set; }
    public string DocumentKey { get; set; } = string.Empty;
    public string? Ncf { get; set; }
    public string? EcfTrackId { get; set; }
    public FiscalDocumentStatus Status { get; set; } = FiscalDocumentStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public string? LastError { get; set; }

    public static FiscalDocument ForSale(int companyId, int saleId, string? ncf = null)
    {
        if (companyId <= 0)
            throw new ArgumentOutOfRangeException(nameof(companyId));
        if (saleId <= 0)
            throw new ArgumentOutOfRangeException(nameof(saleId));

        return new FiscalDocument
        {
            CompanyId = companyId,
            SaleId = saleId,
            DocumentKey = $"SALE:{saleId}",
            Ncf = string.IsNullOrWhiteSpace(ncf) ? null : ncf.Trim(),
            Status = FiscalDocumentStatus.Pending
        };
    }
}

public class FiscalSubmissionAudit : BaseEntity
{
    public int CompanyId { get; set; }
    public int FiscalDocumentId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public string? ResponsePayload { get; set; }
    public string? PayloadHash { get; set; }
}
