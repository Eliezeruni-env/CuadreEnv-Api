using System.Collections.Generic;
using System.Threading.Tasks;
using Onion.Domain.Finance;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IAccountReceivableService
    {
        Task<AccountReceivable> CreateFromSaleAsync(Onion.Domain.Sale sale);
        Task RegisterPaymentAsync(int receivableId, decimal amount);
        Task<IEnumerable<AccountReceivable>> GetOverdueAsync();
        Task<IEnumerable<AccountReceivable>> GetDueSoonAsync(int days);
        Task<IEnumerable<AccountReceivable>> GetByCustomerAsync(int customerId);
    }
}
