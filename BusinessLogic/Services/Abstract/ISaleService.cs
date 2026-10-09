using System.Collections.Generic;
using System.Threading.Tasks;
using Onion.Common.Models.Pagination;
using Onion.Domain;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface ISaleService
    {
        Task<IEnumerable<Sale>> GetAllAsync();
        Task<PagedList<Sale>> GetPagedAsync(int pageNumber, int pageSize);
        Task<Sale?> GetByIdAsync(int id);
        Task<Sale> CreateAsync(Sale sale);
        Task UpdateAsync(Sale sale);
        Task DeleteAsync(int id);
        // Payment-related convenience
        Task AddPaymentAsync(int saleId, Payment payment, string? method = null, int? cashRegisterId = null);
        Task CancelAsync(int saleId, string reason);
    }
}
