using System.Threading.Tasks;
using Onion.Domain;
using System.Threading.Tasks;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface ICajaService
    {
        Task<Sale> CreateSaleAsync(Sale sale, string idempotencyKey, int? cashRegisterId = null);
    }
}
