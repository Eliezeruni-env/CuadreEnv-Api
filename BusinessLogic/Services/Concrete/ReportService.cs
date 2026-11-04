using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Concrete;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _uow;

        public ReportService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<object> GetSalesByPeriodAsync(DateTime? from, DateTime? to, string period)
        {
            var q = _uow.Sales.Query().AsNoTracking();
            if (from.HasValue) q = q.Where(s => s.Date >= from.Value);
            if (to.HasValue) q = q.Where(s => s.Date <= to.Value);

            var normalizedPeriod = period?.ToLowerInvariant();
            if (normalizedPeriod == "month")
            {
                return await q.GroupBy(s => new { s.Date.Year, Month = s.Date.Month })
                    .Select(g => new { Period = g.Key.Year + "-" + g.Key.Month, Total = g.Sum(x => x.Total), Count = g.Count(), AvgTicket = g.Average(x => x.Total) })
                    .ToListAsync();
            }

            if (normalizedPeriod == "week")
            {
                var dailyForWeeks = await q.GroupBy(s => new { s.Date.Year, Day = s.Date.DayOfYear })
                    .Select(g => new { g.Key.Year, g.Key.Day, Total = g.Sum(x => x.Total), Count = g.Count() })
                    .ToListAsync();

                return dailyForWeeks
                    .GroupBy(x =>
                    {
                        var date = new DateTime(x.Year, 1, 1).AddDays(x.Day - 1);
                        return new { date.Year, Week = System.Globalization.ISOWeek.GetWeekOfYear(date) };
                    })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Week)
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-W{g.Key.Week:D2}",
                        Total = g.Sum(x => x.Total),
                        Count = g.Sum(x => x.Count),
                        AvgTicket = g.Sum(x => x.Total) / g.Sum(x => x.Count)
                    })
                    .ToList();
            }

            var daily = await q.GroupBy(s => new { s.Date.Year, Day = s.Date.DayOfYear })
                .Select(g => new { g.Key.Year, g.Key.Day, Total = g.Sum(x => x.Total), Count = g.Count(), AvgTicket = g.Average(x => x.Total) })
                .OrderBy(x => x.Year).ThenBy(x => x.Day)
                .ToListAsync();
            return daily.Select(x =>
            {
                var date = new DateTime(x.Year, 1, 1).AddDays(x.Day - 1);
                return new { Period = date.ToString("yyyy-MM-dd"), x.Total, x.Count, x.AvgTicket };
            }).ToList();
        }

        public async Task<object> GetTopProductsAsync(DateTime? from, DateTime? to)
        {
            var details = _uow.Sales.Query().AsNoTracking().SelectMany(s => s.Details.Select(d => new
            {
                s.Date,
                d.ProductId,
                Amount = d.UnitPrice * d.Quantity,
                d.Quantity
            }));
            if (from.HasValue) details = details.Where(d => d.Date >= from.Value);
            if (to.HasValue) details = details.Where(d => d.Date <= to.Value);

            return await details
                .GroupBy(d => d.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity), Amount = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Quantity)
                .Take(20)
                .ToListAsync();
        }

        public async Task<object> GetInventoryStatusAsync()
        {
            var products = _uow.Products.Query().AsNoTracking();
            var totalValue = await products.SumAsync(p => p.Stock * p.Cost);
            var count = await products.CountAsync();
            var low = await products.Where(p => p.Stock < p.MinimumQuantity)
                .Select(p => new { p.Id, p.Description, p.Stock, p.MinimumQuantity })
                .ToListAsync();
            return new { TotalValue = totalValue, Count = count, LowStock = low };
        }

        public async Task<object> GetActiveCustomersAsync(DateTime? from, DateTime? to)
        {
            var q = _uow.Sales.Query().AsNoTracking();
            if (from.HasValue) q = q.Where(s => s.Date >= from.Value);
            if (to.HasValue) q = q.Where(s => s.Date <= to.Value);
            var grouped = q.Where(s => s.CustomerId.HasValue).GroupBy(s => s.CustomerId).Select(g => new { CustomerId = g.Key, Total = g.Sum(x => x.Total), Count = g.Count() }).OrderByDescending(x => x.Total).Take(50);
            return await grouped.ToListAsync();
        }

        public async Task<object> GetAccountsReceivableSummaryAsync(DateTime? from, DateTime? to)
        {
            var q = _uow.AccountReceivables.Query().AsNoTracking();
            if (from.HasValue) q = q.Where(a => a.CreationDate >= from.Value);
            if (to.HasValue) q = q.Where(a => a.CreationDate <= to.Value);
            var totalPending = await q.SumAsync(a => a.Balance);
            var overdue = await q.Where(a => a.DueDate < DateTime.UtcNow).SumAsync(a => a.Balance);
            var byCustomer = q.GroupBy(a => a.CustomerId).Select(g => new { CustomerId = g.Key, TotalPending = g.Sum(x => x.Balance) }).OrderByDescending(x => x.TotalPending);
            return new { TotalPending = totalPending, Overdue = overdue, ByCustomer = await byCustomer.ToListAsync() };
        }
    }
}
