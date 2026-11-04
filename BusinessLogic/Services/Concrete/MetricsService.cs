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
            var sales = _db.Sales.AsNoTracking().Where(s => s.Date >= from && s.Date < to);
            var revenue = await sales.SumAsync(s => (decimal?)s.Total, cancellationToken) ?? 0m;
            var salesCount = await sales.CountAsync(cancellationToken);
            var previousRevenue = await _db.Sales.AsNoTracking()
                .Where(s => s.Date >= previousFrom && s.Date < from)
                .SumAsync(s => (decimal?)s.Total, cancellationToken) ?? 0m;
            var customerCount = await sales.Where(s => s.CustomerId.HasValue)
                .Select(s => s.CustomerId!.Value)
                .Distinct()
                .CountAsync(cancellationToken);

            var payments = _db.Payments.AsNoTracking()
                .Where(p => p.SaleId.HasValue)
                .Join(sales, p => p.SaleId!.Value, s => s.Id, (p, _) => p);
            var paymentTotals = await payments
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new { Method = g.Key, Amount = g.Sum(p => p.Amount) })
                .ToListAsync(cancellationToken);
            var cashIn = paymentTotals.FirstOrDefault(x => x.Method == PaymentMethod.CASH)?.Amount ?? 0m;
            var detailsQuantity = await _db.SaleDetails.AsNoTracking()
                .Join(sales, d => d.SaleId, s => s.Id, (d, _) => d.Quantity)
                .SumAsync(cancellationToken);
        var methods = new Dictionary<string, PaymentMethodMetric>(StringComparer.OrdinalIgnoreCase);
        foreach (var method in Enum.GetValues<PaymentMethod>())
        {
                var amount = paymentTotals.FirstOrDefault(x => x.Method == method)?.Amount ?? 0m;
            methods[method.ToString().ToLowerInvariant()] = new PaymentMethodMetric(amount, revenue == 0 ? 0 : Math.Round(amount / revenue * 100m, 2));
        }
            return new MetricsSummaryDto(revenue, salesCount, salesCount == 0 ? 0 : revenue / salesCount, cashIn, 0m, customerCount, detailsQuantity, previousRevenue == 0 ? 0 : Math.Round((revenue - previousRevenue) / previousRevenue * 100m, 2), methods);
    }

    public async Task<IReadOnlyList<SalesEvolutionPoint>> GetSalesEvolutionAsync(string? period, CancellationToken cancellationToken = default)
    {
        var (from, to, _) = GetRange(period);
        var monthly = string.Equals(period, "1A", StringComparison.OrdinalIgnoreCase) || string.Equals(period, "year", StringComparison.OrdinalIgnoreCase);
            var sales = _db.Sales.AsNoTracking().Where(s => s.Date >= from && s.Date < to);
            if (monthly)
            {
                var groupedByMonth = await sales
                    .GroupBy(s => new { s.Date.Year, Month = s.Date.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(s => s.Total), Count = g.Count() })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToListAsync(cancellationToken);
                return groupedByMonth.Select(x => new SalesEvolutionPoint(
                    $"{x.Year:D4}-{x.Month:D2}", x.Revenue, x.Count)).ToList();
            }

            var groupedByDay = await sales
                .GroupBy(s => new { s.Date.Year, Day = s.Date.DayOfYear })
                .Select(g => new { g.Key.Year, g.Key.Day, Revenue = g.Sum(s => s.Total), Count = g.Count() })
                .OrderBy(x => x.Year).ThenBy(x => x.Day)
                .ToListAsync(cancellationToken);
            return groupedByDay.Select(x => new SalesEvolutionPoint(
                new DateTime(x.Year, 1, 1).AddDays(x.Day - 1).ToString("yyyy-MM-dd"),
                x.Revenue, x.Count)).ToList();
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
