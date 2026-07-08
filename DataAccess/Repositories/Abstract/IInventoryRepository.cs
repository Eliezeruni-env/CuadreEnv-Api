using System.Threading.Tasks;
using Onion.Domain.Warehouses;

namespace Onion.DataAccess.Repositories.Abstract
{
    public interface IInventoryRepository : IRepository<Inventory>
    {
        Task<Inventory?> GetByProductAndWarehouseAsync(int productId, int warehouseId);
    }
}
