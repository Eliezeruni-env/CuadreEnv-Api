using System.Threading.Tasks;
using Onion.Domain.Warehouses;

namespace Onion.DataAccess.Repositories.Abstract
{
    public interface IInventoryRepository : IRepository<Inventory>
    {
        Task<Inventory?> GetByProductAndWarehouseAsync(int productId, int warehouseId);
        Task<bool> TryIncreaseStockAsync(int productId, int warehouseId, int companyId, decimal quantity, string performedBy);
        Task<bool> TryReduceStockAsync(int productId, int warehouseId, decimal quantity, bool allowNegativeStock = false);
    }
}
