using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess;
using Onion.Domain;
using Microsoft.EntityFrameworkCore;

namespace Onion.BussinesLogic.Services.Concrete;

public sealed class MetricsService : IMetricsService
{
    private readonly OnionDbContext _db;

    public MetricsService(OnionDbContext db) => _db = db;

    public async Task<MetricsSummaryDto> GetSummaryAsync(string? period, CancellationToken cancellationToken = default)
    {
        var (from, to, previousFrom) = GetRange(period);
        var sales = await _db.Sales.AsNoTracking().Where(s => s.Date >= from && s.Date < to).ToListAsync(cancellationToken);
        var previousRevenue = await _db.Sales.AsNoTracking().Where(s => s.Date >= previousFrom && s.Date < from).SumAsync(s => (decimal?)s.Total, cancellationToken) ?? 0m;
        var revenue = sales.Sum(s => s.Total);
        var payments = await _db.Payments.AsNoTracking().Where(p => p.SaleId != null && sales.Select(s => s.Id).Contains(p.SaleId.Value)).ToListAsync(cancellationToken);
        var details = await _db.SaleDetails.AsNoTracking().Where(d => sales.Select(s => s.Id).Contains(d.SaleId)).ToListAsync(cancellationToken);
        var customerCount = sales.Where(s => s.CustomerId.HasValue).Select(s => s.CustomerId!.Value).Distinct().Count();
        var cashIn = payments.Where(p => p.PaymentMethod == PaymentMethod.CASH).Sum(p => p.Amount);
        var methods = new Dictionary<string, PaymentMethodMetric>(StringComparer.OrdinalIgnoreCase);
        foreach (var method in Enum.GetValues<PaymentMethod>())
        {
            var amount = payments.Where(p => p.PaymentMethod == method).Sum(p => p.Amount);
            methods[method.ToString().ToLowerInvariant()] = new PaymentMethodMetric(amount, revenue == 0 ? 0 : Math.Round(amount / revenue * 100m, 2));
        }
        return new MetricsSummaryDto(revenue, sales.Count, sales.Count == 0 ? 0 : revenue / sales.Count, cashIn, 0m, customerCount, details.Sum(d => d.Quantity), previousRevenue == 0 ? 0 : Math.Round((revenue - previousRevenue) / previousRevenue * 100m, 2), methods);
    }

    public async Task<IReadOnlyList<SalesEvolutionPoint>> GetSalesEvolutionAsync(string? period, CancellationToken cancellationToken = default)
    {
        var (from, to, _) = GetRange(period);
        var sales = await _db.Sales.AsNoTracking().Where(s => s.Date >= from && s.Date < to).ToListAsync(cancellationToken);
        var monthly = string.Equals(period, "1A", StringComparison.OrdinalIgnoreCase) || string.Equals(period, "year", StringComparison.OrdinalIgnoreCase);
        return sales.GroupBy(s => monthly ? s.Date.ToString("yyyy-MM") : s.Date.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key).Select(g => new SalesEvolutionPoint(g.Key, g.Sum(s => s.Total), g.Count())).ToList();
    }

    public async Task<IReadOnlyList<TopProductMetric>> GetTopProductsAsync(int limit, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        var rows = await (from detail in _db.SaleDetails.AsNoTracking()
                          join sale in _db.Sales.AsNoTracking() on detail.SaleId equals sale.Id
                          join product in _db.Products.AsNoTracking() on detail.ProductId equals product.Id
                          group new { detail, product } by new { product.Id, product.Description } into g
                          select new { g.Key.Id, Name = g.Key.Description, Units = g.Sum(x => x.detail.Quantity), Revenue = g.Sum(x => x.detail.Quantity * x.detail.UnitPrice) })
            .OrderByDescending(x => x.Units).Take(limit).ToListAsync(cancellationToken);
        var total = rows.Sum(x => x.Units);
        return rows.Select(x => new TopProductMetric(x.Id, x.Name, x.Units, x.Revenue, total == 0 ? 0 : Math.Round(x.Units / total * 100m, 2))).ToList();
    }

    private static (DateTime From, DateTime To, DateTime PreviousFrom) GetRange(string? period)
    {
        var to = DateTime.UtcNow;
        var span = period?.ToLowerInvariant() switch
        {
            "month" => TimeSpan.FromDays(30),
            "30d" => TimeSpan.FromDays(30),
            "90d" => TimeSpan.FromDays(90),
            "1a" or "year" => TimeSpan.FromDays(365),
            _ => TimeSpan.FromDays(7)
        };
        var from = to.Subtract(span);
        return (from, to, from.Subtract(span));
    }
}
