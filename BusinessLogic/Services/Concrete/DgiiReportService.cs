using Microsoft.EntityFrameworkCore;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess;
using Onion.Domain;

namespace Onion.BussinesLogic.Services.Concrete;

public sealed class DgiiReportService : IDgiiReportService
{
    private readonly OnionDbContext _db;

    public DgiiReportService(OnionDbContext db) => _db = db;

    public async Task<DgiiReportResult> Get607Async(int companyId, int year, int month, CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var sales = await _db.Sales.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Date >= start && x.Date < end && x.Status != SaleStatus.CANCELLED)
            .OrderBy(x => x.Date).ToListAsync(cancellationToken);
        var customerIds = sales.Where(x => x.CustomerId.HasValue).Select(x => x.CustomerId!.Value).Distinct().ToList();
        var customers = await _db.Customers.AsNoTracking().Where(x => customerIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        var saleIds = sales.Select(x => x.Id).ToList();
        var payments = await _db.Payments.AsNoTracking().Where(x => x.SaleId.HasValue && saleIds.Contains(x.SaleId.Value)).ToListAsync(cancellationToken);

        var rows = sales.Select(s =>
        {
            customers.TryGetValue(s.CustomerId ?? 0, out var customer);
            var identification = customer?.Identification ?? string.Empty;
            var typeId = identification.Length == 11 ? "1" : identification.Length == 9 ? "2" : string.Empty;
            var salePayments = payments.Where(x => x.SaleId == s.Id).ToList();
            decimal ByMethod(PaymentMethod method) => salePayments.Where(x => x.PaymentMethod == method).Sum(x => x.Amount);
            var cash = ByMethod(PaymentMethod.CASH);
            var card = ByMethod(PaymentMethod.CARD);
            var transfer = ByMethod(PaymentMethod.TRANSFER);
            if (salePayments.Count == 0 && s.PaymentType == PaymentType.CASH) cash = s.PaidAmount;
            var credit = s.PaymentType == PaymentType.CREDIT ? Math.Max(0, s.Total - s.PaidAmount) : 0m;
            return new[]
            {
                identification, typeId, s.InvoiceFolio ?? string.Empty, string.Empty, s.Date.ToString("yyyyMMdd"), string.Empty,
                s.SubTotal == 0 ? s.Total.ToString("0.00") : s.SubTotal.ToString("0.00"), s.Tax.ToString("0.00"), s.TaxWithheld.ToString("0.00"), "0.00",
                cash.ToString("0.00"), card.ToString("0.00"), transfer.ToString("0.00"), credit.ToString("0.00"), "0.00"
            };
        }).ToList();
        return new DgiiReportResult("607", year, month, rows);
    }

    public async Task<DgiiReportResult> Get606Async(int companyId, int year, int month, CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var purchases = await _db.Purchases.AsNoTracking().Where(x => x.CompanyId == companyId && x.CreationDate >= start && x.CreationDate < end).OrderBy(x => x.CreationDate).ToListAsync(cancellationToken);
        var suppliers = await _db.Suppliers.AsNoTracking().Where(x => x.CompanyId == companyId).ToDictionaryAsync(x => x.Id, cancellationToken);
        var rows = purchases.Select(p =>
        {
            suppliers.TryGetValue(p.SupplierId, out var supplier);
            var identification = supplier?.RncOrId ?? string.Empty;
            var typeId = identification.Length == 11 ? "1" : identification.Length == 9 ? "2" : string.Empty;
            return new[]
            {
                identification, typeId, string.Empty, string.Empty, p.CreationDate.ToString("yyyyMMdd"),
                p.Total.ToString("0.00"), "0.00", "0.00", "0.00", "0.00", "0.00", "0.00", "0.00", "0.00", "0.00"
            };
        }).ToList();
        return new DgiiReportResult("606", year, month, rows);
    }

    public async Task<DgiiReportResult> Get608Async(int companyId, int year, int month, CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var sales = await _db.Sales.AsNoTracking().Where(x => x.CompanyId == companyId && x.Status == SaleStatus.CANCELLED && x.Date >= start && x.Date < end).OrderBy(x => x.Date).ToListAsync(cancellationToken);
        var rows = sales.Select(x => new[] { x.InvoiceFolio ?? string.Empty, x.Date.ToString("yyyyMMdd"), x.Notes ?? "ANULACION" }).ToList();
        return new DgiiReportResult("608", year, month, rows);
    }

    private static void ValidatePeriod(int year, int month)
    {
        if (year < 2000 || year > 2100) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
    }
}
