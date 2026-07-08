using System.Collections.Generic;
using System.Threading.Tasks;
using Onion.Domain;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface ISaleService
    {
        Task<IEnumerable<Sale>> GetAllAsync();
        Task<Sale?> GetByIdAsync(int id);
        Task<Sale> CreateAsync(Sale sale);
        Task UpdateAsync(Sale sale);
        Task DeleteAsync(int id);
        // Payment-related convenience
        Task AddPaymentAsync(int saleId, Payment payment);
    }
}
