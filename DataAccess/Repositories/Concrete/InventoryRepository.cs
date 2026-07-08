using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain.Warehouses;

namespace Onion.DataAccess.Repositories.Concrete
{
    public class InventoryRepository : GenericRepository<Inventory>, IInventoryRepository
    {
        public InventoryRepository(OnionDbContext context) : base(context) { }

        public async Task<Inventory?> GetByProductAndWarehouseAsync(int productId, int warehouseId)
        {
            return await _context.Set<Inventory>().AsNoTracking().FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);
        }
    }
}
