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

        public async Task<bool> TryReduceStockAsync(int productId, int warehouseId, decimal quantity, bool allowNegativeStock = false)
        {
            var query = _context.Set<Inventory>()
                .Where(i => i.ProductId == productId && i.WarehouseId == warehouseId)
                .AsQueryable();

            if (!allowNegativeStock)
            {
                query = query.Where(i => i.Quantity >= quantity);
            }

            if (!_context.IsGlobalTenantAccess)
            {
                if (!_context.TenantCompanyId.HasValue)
                    throw new System.InvalidOperationException("Tenant company id missing for multi-tenant operation.");

                var companyId = _context.TenantCompanyId.Value;
                query = query.Where(i => i.CompanyId == companyId);
            }

            var affected = await query
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Quantity, i => i.Quantity - quantity));

            return affected > 0;
        }
    }
}
