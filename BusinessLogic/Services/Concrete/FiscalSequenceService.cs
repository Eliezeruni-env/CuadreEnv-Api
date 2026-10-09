using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Onion.Common.Exceptions;
using Onion.DataAccess;
using Onion.Domain.Invoices;

namespace Onion.BussinesLogic.Services.Concrete;

public sealed class FiscalSequenceService : Onion.BussinesLogic.Services.Abstract.IFiscalSequenceService
{
    private readonly OnionDbContext _db;
    private readonly ILogger<FiscalSequenceService> _logger;

    public FiscalSequenceService(OnionDbContext db, ILogger<FiscalSequenceService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<string> NextFiscalNumberAsync(int companyId, VoucherType voucherType, CancellationToken cancellationToken = default)
    {
        if (companyId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));

        var ownsTransaction = _db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            var sequence = await _db.FiscalSequences
                .FromSqlInterpolated<FiscalSequence>($"SELECT * FROM [FiscalSequences] WITH (UPDLOCK, ROWLOCK) WHERE [CompanyId] = {companyId} AND [VoucherType] = {(int)voucherType} AND [IsActive] = 1")
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(cancellationToken);

            if (sequence is null)
                throw new FiscalSequenceExhaustedException($"Secuencia fiscal {voucherType} agotada o vencida ante la DGII. Solicite nueva autorización.");
            if (sequence.ExpirationDate.ToUniversalTime() < DateTime.UtcNow)
                throw new FiscalSequenceExpiredException($"Secuencia fiscal {sequence.Prefix} agotada o vencida ante la DGII. Solicite nueva autorización.");
            if (sequence.CurrentNumber > sequence.ToNumber)
                throw new FiscalSequenceExhaustedException($"Secuencia fiscal {sequence.Prefix} agotada o vencida ante la DGII. Solicite nueva autorización.");

            var value = sequence.CurrentNumber;
            sequence.CurrentNumber = checked(value + 1);
            if (sequence.CurrentNumber > sequence.ToNumber)
                sequence.IsActive = false;

            _db.FiscalSequences.Update(sequence);
            await _db.SaveChangesAsync(cancellationToken);
            if (ownsTransaction)
                await transaction!.CommitAsync(cancellationToken);

            var digits = sequence.IsElectronic ? 10 : 8;
            return $"{sequence.Prefix.Trim().ToUpperInvariant()}{value.ToString($"D{digits}")}";
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (ownsTransaction)
                await transaction!.RollbackAsync(cancellationToken);
            _logger.LogWarning(ex, "Fiscal sequence concurrency conflict for company {CompanyId}, type {VoucherType}", companyId, voucherType);
            throw new FiscalSequenceExhaustedException("The fiscal sequence was changed concurrently. Retry the operation.");
        }
        catch
        {
            if (ownsTransaction)
                await transaction!.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<Onion.BussinesLogic.Services.Abstract.FiscalSequenceSummaryDto>> GetActiveAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _db.FiscalSequences
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.VoucherType)
            .Select(x => new Onion.BussinesLogic.Services.Abstract.FiscalSequenceSummaryDto(
                x.Id, x.VoucherType, x.Prefix, x.CurrentNumber, x.FromNumber, x.ToNumber,
                x.ToNumber - x.CurrentNumber + 1,
                x.ExpirationDate,
                x.ExpirationDate <= now ? 0 : (x.ExpirationDate.Date - now.Date).Days,
                x.IsActive,
                x.ToNumber - x.CurrentNumber + 1 <= x.WarningThreshold))
            .ToListAsync(cancellationToken);
    }

    public async Task<Onion.BussinesLogic.Services.Abstract.FiscalSequenceSummaryDto> CreateAsync(int companyId, Onion.BussinesLogic.Services.Abstract.CreateFiscalSequenceRequest request, CancellationToken cancellationToken = default)
    {
        if (request.FromNumber < 0 || request.ToNumber < request.FromNumber)
            throw new ArgumentException("The fiscal range is invalid.");
        var prefix = request.Prefix.Trim().ToUpperInvariant();
        if (!Enum.IsDefined(request.VoucherType) || prefix.Length != 3 || !prefix.StartsWith("B") && !prefix.StartsWith("E"))
            throw new ArgumentException("The fiscal prefix or voucher type is invalid.");
        if (request.ExpirationDate.ToUniversalTime() <= DateTime.UtcNow)
            throw new ArgumentException("The fiscal sequence must expire in the future.");

        var exists = await _db.FiscalSequences.IgnoreQueryFilters().AnyAsync(x => x.CompanyId == companyId && x.VoucherType == request.VoucherType && x.IsActive, cancellationToken);
        if (exists) throw new InvalidOperationException("An active sequence already exists for this voucher type.");

        var entity = new FiscalSequence
        {
            CompanyId = companyId,
            VoucherType = request.VoucherType,
            Prefix = prefix,
            CurrentNumber = request.FromNumber,
            FromNumber = request.FromNumber,
            ToNumber = request.ToNumber,
            ExpirationDate = request.ExpirationDate.ToUniversalTime(),
            WarningThreshold = Math.Max(0, request.WarningThreshold),
            IsActive = true
        };
        entity.Validate();
        await _db.FiscalSequences.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return new Onion.BussinesLogic.Services.Abstract.FiscalSequenceSummaryDto(entity.Id, entity.VoucherType, entity.Prefix, entity.CurrentNumber, entity.FromNumber, entity.ToNumber, entity.Remaining, entity.ExpirationDate, Math.Max(0, (entity.ExpirationDate.Date - DateTime.UtcNow.Date).Days), entity.IsActive, entity.Remaining <= entity.WarningThreshold);
    }
}
