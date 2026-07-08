using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            var sales = await _uow.Sales.ListAsync();
            var q = sales.AsQueryable();
            if (from.HasValue) q = q.Where(s => s.Date >= from.Value);
            if (to.HasValue) q = q.Where(s => s.Date <= to.Value);

            var grouped = period.ToLower() switch
            {
                "month" => q.GroupBy(s => new { s.Date.Year, Month = s.Date.Month }).Select(g => new { Period = g.Key.Year + "-" + g.Key.Month, Total = g.Sum(x => x.Total), Count = g.Count(), AvgTicket = g.Average(x => x.Total) }),
                "week" => q.GroupBy(s => new { s.Date.Year, Week = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(s.Date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) }).Select(g => new { Period = g.Key.Year + "-W" + g.Key.Week, Total = g.Sum(x => x.Total), Count = g.Count(), AvgTicket = g.Average(x => x.Total) }),
                _ => q.GroupBy(s => s.Date.Date).Select(g => new { Period = g.Key.ToString("yyyy-MM-dd"), Total = g.Sum(x => x.Total), Count = g.Count(), AvgTicket = g.Average(x => x.Total) })
            };

            return grouped.ToList();
        }

        public async Task<object> GetTopProductsAsync(DateTime? from, DateTime? to)
        {
            var sales = await _uow.Sales.ListAsync();
            var details = sales.SelectMany(s => s.Details.Select(d => new { s.Date, d.ProductId, Amount = d.UnitPrice * d.Quantity, Quantity = d.Quantity }));
            if (from.HasValue) details = details.Where(d => d.Date >= from.Value);
            if (to.HasValue) details = details.Where(d => d.Date <= to.Value);

            var byQty = details.GroupBy(d => d.ProductId).Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity), Amount = g.Sum(x => x.Amount) }).OrderByDescending(x => x.Quantity).Take(20);
            return byQty.ToList();
        }

        public async Task<object> GetInventoryStatusAsync()
        {
            var products = await _uow.Products.ListAsync();
            var totalValue = products.Sum(p => p.Stock * (decimal)p.Cost);
            var low = products.Where(p => p.Stock < p.MinimumQuantity).Select(p => new { p.Id, p.Description, p.Stock, p.MinimumQuantity });
            return new { TotalValue = totalValue, Count = products.Count(), LowStock = low.ToList() };
        }

        public async Task<object> GetActiveCustomersAsync(DateTime? from, DateTime? to)
        {
            var sales = await _uow.Sales.ListAsync();
            var q = sales.AsQueryable();
            if (from.HasValue) q = q.Where(s => s.Date >= from.Value);
            if (to.HasValue) q = q.Where(s => s.Date <= to.Value);
            var grouped = q.Where(s => s.CustomerId.HasValue).GroupBy(s => s.CustomerId).Select(g => new { CustomerId = g.Key, Total = g.Sum(x => x.Total), Count = g.Count() }).OrderByDescending(x => x.Total).Take(50);
            return grouped.ToList();
        }

        public async Task<object> GetAccountsReceivableSummaryAsync(DateTime? from, DateTime? to)
        {
            var ars = await _uow.AccountReceivables.ListAsync();
            var q = ars.AsQueryable();
            if (from.HasValue) q = q.Where(a => a.CreationDate >= from.Value);
            if (to.HasValue) q = q.Where(a => a.CreationDate <= to.Value);
            var totalPending = q.Sum(a => a.Balance);
            var overdue = q.Where(a => a.DueDate < DateTime.UtcNow).Sum(a => a.Balance);
            var byCustomer = q.GroupBy(a => a.CustomerId).Select(g => new { CustomerId = g.Key, TotalPending = g.Sum(x => x.Balance) }).OrderByDescending(x => x.TotalPending);
            return new { TotalPending = totalPending, Overdue = overdue, ByCustomer = byCustomer.ToList() };
        }
    }
}
