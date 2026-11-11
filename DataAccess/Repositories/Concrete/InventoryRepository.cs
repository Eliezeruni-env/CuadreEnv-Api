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

        public async Task<bool> TryIncreaseStockAsync(int productId, int warehouseId, int companyId, decimal quantity, string performedBy)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == productId && p.CompanyId == companyId);
            if (!productExists)
                return false;

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""Inventories""
                    (""ProductId"", ""WarehouseId"", ""Quantity"", ""CompanyId"", ""CreationDate"", ""Active"", ""IsDeleted"", ""CreateBy"", ""ModifiedBy"")
                VALUES ({productId}, {warehouseId}, {quantity}, {companyId}, CURRENT_TIMESTAMP, TRUE, FALSE, {performedBy}, {performedBy})
                ON CONFLICT (""CompanyId"", ""ProductId"", ""WarehouseId"")
                DO UPDATE SET
                    ""Quantity"" = ""Inventories"".""Quantity"" + EXCLUDED.""Quantity"",
                    ""ModificationDate"" = CURRENT_TIMESTAMP,
                    ""ModifiedBy"" = EXCLUDED.""ModifiedBy"";");

            return true;
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
