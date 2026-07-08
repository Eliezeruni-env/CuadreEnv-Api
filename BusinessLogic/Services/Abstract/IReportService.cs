using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IReportService
    {
        Task<object> GetSalesByPeriodAsync(DateTime? from, DateTime? to, string period);
        Task<object> GetTopProductsAsync(DateTime? from, DateTime? to);
        Task<object> GetInventoryStatusAsync();
        Task<object> GetActiveCustomersAsync(DateTime? from, DateTime? to);
        Task<object> GetAccountsReceivableSummaryAsync(DateTime? from, DateTime? to);
    }
}
