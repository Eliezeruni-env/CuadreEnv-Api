using Onion.Domain.Finance;

namespace Onion.BussinesLogic.Services.Abstract;

public interface ICashSessionService
{
    Task<CashSessionDto> OpenAsync(int companyId, OpenCashSessionRequest request, int cashierUserId, CancellationToken cancellationToken = default);
    Task<CashSessionDto?> GetActiveAsync(int companyId, int cashierUserId, CancellationToken cancellationToken = default);
    Task<CashMovementDto> AddMovementAsync(int companyId, int sessionId, AddCashMovementRequest request, int userId, CancellationToken cancellationToken = default);
    Task<CashSessionDto> CloseAsync(int companyId, int sessionId, CloseCashSessionRequest request, int userId, CancellationToken cancellationToken = default);
    Task<CashSessionDto> CloseByRegisterOrSessionAsync(int companyId, int id, CloseCashSessionRequest request, int userId, CancellationToken cancellationToken = default);
}

public sealed record OpenCashSessionRequest(int CashierUserId, decimal OpeningAmount, object? OpeningDenominations, int? CashRegisterId = null);
public sealed record AddCashMovementRequest(CashMovementType Type, decimal Amount, string Reason, int? AuthorizedBySupervisorId = null);
public class CloseCashSessionRequest
{
    public decimal ActualAmount { get; set; }
    public string? BreakdownJson { get; set; }
    public string? Notes { get; set; }
    public decimal DeclaredCards { get; set; }
    public string? CardBatchNumber { get; set; }
    public decimal DeclaredTransfers { get; set; }
    public int TransferVoucherCount { get; set; }
    public decimal? RetainedFloat { get; set; }
    public decimal? CashToDeposit { get; set; }
    public string? DepositDestination { get; set; }
    public string? SupervisorPin { get; set; }
    public int? SupervisorUserId { get; set; }
    public string? ClosingComment { get; set; }

    // Backward-compatible aliases used by the original cash-session client.
    public decimal DeclaredCash
    {
        get => ActualAmount;
        set => ActualAmount = value;
    }

    public object? Denominations { get; set; }

    public string? SupervisorNotes
    {
        get => Notes;
        set => Notes = value;
    }
}
public sealed record CashMovementDto(int Id, int SessionId, CashMovementType Type, decimal Amount, string Reason, DateTime RecordedAt, decimal CurrentBalance = 0);
public sealed record CashRegisterCloseResult(
    decimal ExpectedAmount,
    decimal ActualAmount,
    decimal Difference,
    string Status,
    bool IsBalanced,
    bool RequiresSupervisorAuth);
public sealed record CashSessionDto(
    int Id,
    int CashRegisterId,
    int CashierUserId,
    decimal OpeningAmount,
    CashSessionStatus Status,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    decimal? ExpectedCash,
    decimal? DeclaredCash,
    decimal? Difference,
    CashClosingStatus? ClosingStatus,
    decimal TotalCashSales = 0,
    decimal TotalCardSales = 0,
    decimal TotalTransferSales = 0,
    decimal TotalCashWithdrawals = 0,
    decimal? ExpectedAmount = null,
    decimal? ActualAmount = null,
    decimal? Diff = null,
    string? Notes = null,
    decimal DeclaredCards = 0,
    string? CardBatchNumber = null,
    decimal DeclaredTransfers = 0,
    int TransferVoucherCount = 0,
    decimal RetainedFloat = 0,
    decimal CashToDeposit = 0,
    string? DepositDestination = null,
    bool SupervisorAuthorized = false);
