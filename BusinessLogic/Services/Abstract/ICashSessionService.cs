using Onion.Domain.Finance;

namespace Onion.BussinesLogic.Services.Abstract;

public interface ICashSessionService
{
    Task<CashSessionDto> OpenAsync(int companyId, OpenCashSessionRequest request, int cashierUserId, CancellationToken cancellationToken = default);
    Task<CashSessionDto?> GetActiveAsync(int companyId, int cashierUserId, CancellationToken cancellationToken = default);
    Task<CashMovementDto> AddMovementAsync(int companyId, int sessionId, AddCashMovementRequest request, int userId, CancellationToken cancellationToken = default);
    Task<CashSessionDto> CloseAsync(int companyId, int sessionId, CloseCashSessionRequest request, int userId, CancellationToken cancellationToken = default);
}

public sealed record OpenCashSessionRequest(int CashierUserId, decimal OpeningAmount, object? OpeningDenominations, int? CashRegisterId = null);
public sealed record AddCashMovementRequest(CashMovementType Type, decimal Amount, string Reason, int? AuthorizedBySupervisorId = null);
public sealed record CloseCashSessionRequest(decimal DeclaredCash, decimal DeclaredCards, decimal DeclaredTransfers, object? Denominations, int? SupervisorUserId, string? SupervisorNotes, string? SupervisorPin = null);
public sealed record CashMovementDto(int Id, int SessionId, CashMovementType Type, decimal Amount, string Reason, DateTime RecordedAt);
public sealed record CashSessionDto(int Id, int CashRegisterId, int CashierUserId, decimal OpeningAmount, CashSessionStatus Status, DateTime OpenedAt, DateTime? ClosedAt, decimal? ExpectedCash, decimal? DeclaredCash, decimal? Difference, CashClosingStatus? ClosingStatus);
