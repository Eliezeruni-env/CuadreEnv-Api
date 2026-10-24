using System.Collections.Generic;
using System.Threading.Tasks;
using Onion.Domain;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface ICajaService
    {
        Task<Sale> CreateSaleAsync(Sale sale, string idempotencyKey, int? cashRegisterId = null, IReadOnlyCollection<Payment>? payments = null);
    }
}
