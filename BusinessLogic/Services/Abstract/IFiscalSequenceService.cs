using Onion.Domain.Invoices;

namespace Onion.BussinesLogic.Services.Abstract;

public interface IFiscalSequenceService
{
    Task<string> NextFiscalNumberAsync(int companyId, VoucherType voucherType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FiscalSequenceSummaryDto>> GetActiveAsync(int companyId, CancellationToken cancellationToken = default);
    Task<FiscalSequenceSummaryDto> CreateAsync(int companyId, CreateFiscalSequenceRequest request, CancellationToken cancellationToken = default);
}

public sealed record CreateFiscalSequenceRequest(
    VoucherType VoucherType,
    string Prefix,
    long FromNumber,
    long ToNumber,
    DateTime ExpirationDate,
    int WarningThreshold = 50);

public sealed record FiscalSequenceSummaryDto(
    int Id,
    VoucherType VoucherType,
    string Prefix,
    long CurrentNumber,
    long FromNumber,
    long ToNumber,
    long Remaining,
    DateTime ExpirationDate,
    int DaysToExpire,
    bool IsActive,
    bool IsLow)
{
    public string TypeCode => VoucherType.ToString();
    public long StartNumber => FromNumber;
    public long EndNumber => ToNumber;
    public long CurrentNumberValue => CurrentNumber;
    public long RemainingCount => Remaining;
    public bool IsWarning => IsLow || Remaining <= 50 || DaysToExpire <= 7;
    public bool IsCritical => Remaining <= 0 || DaysToExpire <= 0;
}
