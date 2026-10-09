using Microsoft.EntityFrameworkCore;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess;
using Onion.Domain.Finance;

namespace Onion.BussinesLogic.Services.Concrete;

public sealed class TaxpayerService : ITaxpayerService
{
    private readonly OnionDbContext _db;

    public TaxpayerService(OnionDbContext db) => _db = db;

    public async Task<TaxpayerDto?> GetAsync(string rncOrCedula, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(rncOrCedula);
        if (normalized.Length == 0) return null;
        var item = await _db.Taxpayers.AsNoTracking().SingleOrDefaultAsync(x => x.RncOrCedula == normalized, cancellationToken);
        return item is null ? null : ToDto(item);
    }

    public async Task<TaxpayerDto> UpsertAsync(Taxpayer taxpayer, CancellationToken cancellationToken = default)
    {
        taxpayer.RncOrCedula = Normalize(taxpayer.RncOrCedula);
        if (taxpayer.RncOrCedula.Length is < 9 or > 11) throw new ArgumentException("RNC or cédula must contain between 9 and 11 digits.");
        var current = await _db.Taxpayers.SingleOrDefaultAsync(x => x.RncOrCedula == taxpayer.RncOrCedula, cancellationToken);
        if (current is null)
        {
            taxpayer.LastSynchronizedAt = DateTime.UtcNow;
            await _db.Taxpayers.AddAsync(taxpayer, cancellationToken);
            current = taxpayer;
        }
        else
        {
            current.BusinessName = taxpayer.BusinessName.Trim();
            current.CommercialName = taxpayer.CommercialName?.Trim();
            current.Category = taxpayer.Category?.Trim();
            current.Status = taxpayer.Status;
            current.LastSynchronizedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(current);
    }

    private static string Normalize(string value) => new((value ?? string.Empty).Where(char.IsDigit).ToArray());
    private static TaxpayerDto ToDto(Taxpayer item) => new(item.RncOrCedula, item.BusinessName, item.CommercialName, item.Category, item.Status == TaxpayerStatus.Active ? "ACTIVO" : "SUSPENDIDO");
}
