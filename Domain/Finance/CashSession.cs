using System.Text.Json;
using Onion.Domain;

namespace Onion.Domain.Finance;

public enum CashSessionStatus
{
    Open = 1,
    Closed = 2
}

public enum CashMovementType
{
    CashIn = 1,
    CashOut = 2,
    Sale = 3,
    ReceivablePayment = 4,
    Refund = 5
}

public enum CashClosingStatus
{
    Exact = 1,
    Shortage = 2,
    Surplus = 3
}

public class CashSession : BaseEntity
{
    public int CompanyId { get; set; }
    public int CashierUserId { get; set; }
    public int CashRegisterId { get; set; }
    public decimal OpeningAmount { get; set; }
    public string OpeningDenominationsJson { get; set; } = "{}";
    public CashSessionStatus Status { get; private set; } = CashSessionStatus.Open;
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; private set; }
    public decimal? ExpectedCash { get; private set; }
    public decimal? DeclaredCash { get; private set; }
    public decimal? DeclaredCards { get; private set; }
    public decimal? DeclaredTransfers { get; private set; }
    public decimal? Difference { get; private set; }
    public CashClosingStatus? ClosingStatus { get; private set; }
    public string? ClosingDenominationsJson { get; private set; }
    public int? SupervisorUserId { get; private set; }
    public string? SupervisorNotes { get; private set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public void Close(decimal expectedCash, decimal declaredCash, decimal declaredCards, decimal declaredTransfers,
        string denominationsJson, CashClosingStatus status, int? supervisorUserId, string? supervisorNotes)
    {
        if (Status != CashSessionStatus.Open) throw new InvalidOperationException("Cash session is already closed.");
        if (expectedCash < 0 || declaredCash < 0 || declaredCards < 0 || declaredTransfers < 0)
            throw new ArgumentOutOfRangeException(nameof(declaredCash));

        Status = CashSessionStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        ExpectedCash = decimal.Round(expectedCash, 2);
        DeclaredCash = decimal.Round(declaredCash, 2);
        DeclaredCards = decimal.Round(declaredCards, 2);
        DeclaredTransfers = decimal.Round(declaredTransfers, 2);
        Difference = decimal.Round(declaredCash - expectedCash, 2);
        ClosingStatus = status;
        ClosingDenominationsJson = string.IsNullOrWhiteSpace(denominationsJson) ? "{}" : denominationsJson;
        SupervisorUserId = supervisorUserId;
        SupervisorNotes = supervisorNotes?.Trim();
    }

    public static string NormalizeDenominations(object denominations) =>
        JsonSerializer.Serialize(denominations);
}
