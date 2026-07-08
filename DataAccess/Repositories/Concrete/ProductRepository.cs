using Microsoft.EntityFrameworkCore;
using Onion.DataAccess.Models;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Common.Models.Pagination;
using Onion.Domain.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Onion.DataAccess.Repositories.Concrete
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(OnionDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Product>> SearchByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return Enumerable.Empty<Product>();
            var q = _dbSet.AsQueryable();
            q = q.Where(p => EF.Functions.Like(p.Description, $"%{name}%"));
            return await q.ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
        {
            return await _dbSet.Where(p => p.CategoryId == categoryId).ToListAsync();
        }

        public async Task<Product?> GetWithCategoryAsync(int id)
        {
            // Product entity does not have navigation property for Category in current domain model.
            // Return product; category can be retrieved via CategoryRepository when needed.
            return await _dbSet.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var n = name.Trim();
            return await _dbSet.AnyAsync(p => p.Description == n);
        }

        public async Task<IEnumerable<Product>> GetLowStockAsync(int threshold)
        {
            // Domain model does not track current stock; use MinimumQuantity as a proxy for low-stock alert.
            return await _dbSet.Where(p => p.MinimumQuantity <= threshold).ToListAsync();
        }

        public Task<bool> ExistsByBarcodeAsync(string barcode, int id)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return Task.FromResult(false);

            barcode = barcode.Trim();

            var query = _context.Products.AsQueryable()
                .Where(x => x.Barcode == barcode);

            if (id > 0)
                query = query.Where(x => x.Id != id);

            return query.AnyAsync();
        }

        public async Task<IEnumerable<Product>> SearchByDescriptionAsync(string description)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(description))
            {
                query = query.Where(x =>
                    EF.Functions.Like(x.Description, $"%{description}%"));
            }

            return await query
                .OrderBy(x => x.Description)
                .Take(5)
                .ToListAsync();
        }

        public async Task<PagedList<ProductRow>> GetPagedProductsAsync(FilterPayload filterPayload, CancellationToken ct = default)
        {
            var query = from p in _context.Products
                        select new ProductRow
                        {
                            Id = p.Id,
                            Description = p.Description,
                            Barcode = p.Barcode,
                            Cost = (decimal)p.Cost
                        };

            if (!string.IsNullOrWhiteSpace(filterPayload.Description))
                query = query.Where(p => p.Description.ToLower().Contains(filterPayload.Description.ToLower()));

            if (filterPayload.MinCost.HasValue)
                query = query.Where(p => p.Cost >= filterPayload.MinCost.Value);

            if (filterPayload.MaxCost.HasValue)
                query = query.Where(p => p.Cost <= filterPayload.MaxCost.Value);

            var totalItemCount = await query.CountAsync(ct);
            var pageCount = (int)Math.Ceiling(totalItemCount / (double)filterPayload.PageSize);

            var items = await query
                .OrderByDescending(p => p.Id)
                .Skip((filterPayload.PageNumber - 1) * filterPayload.PageSize)
                .Take(filterPayload.PageSize)
                .ToListAsync(ct);

            return new PagedList<ProductRow>(items, filterPayload.PageSize, pageCount, totalItemCount);
        }

        public async Task<IEnumerable<Product>> GetProductsBySqlAsync(decimal minCost)
        {
            // Ensure tenant scoping: avoid raw SQL that could bypass query filters.
            if (!_context.TenantCompanyId.HasValue) throw new InvalidOperationException("Tenant company id missing for multi-tenant operation.");
            var companyId = _context.TenantCompanyId.Value;
            var minCostDouble = Convert.ToDouble(minCost);
            return await _context.Products
                .Where(p => p.Cost >= minCostDouble && EF.Property<int>(p, "CompanyId") == companyId)
                .ToListAsync();
        }

        public async Task<Product?> GetByIdSqlAsync(int id)
        {
            if (!_context.TenantCompanyId.HasValue) throw new InvalidOperationException("Tenant company id missing for multi-tenant operation.");
            var companyId = _context.TenantCompanyId.Value;
            return await _context.Products.FirstOrDefaultAsync(p => p.Id == id && EF.Property<int>(p, "CompanyId") == companyId);
        }

        public async Task<int> UpdateCostBySqlAsync(int id, decimal newCost)
        {
            if (!_context.TenantCompanyId.HasValue) throw new InvalidOperationException("Tenant company id missing for multi-tenant operation.");
            var companyId = _context.TenantCompanyId.Value;
            // Use LINQ update pattern: load, validate company, update field
            var entity = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && EF.Property<int>(p, "CompanyId") == companyId);
            if (entity == null) return 0;
            entity.Cost = Convert.ToDouble(newCost);
            _context.Products.Update(entity);
            await _context.SaveChangesAsync();
            return 1;
        }

        public async Task UpdateAsync(Product entity, CancellationToken ct = default)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            // Use query that respects global filters to ensure tenant scoping
            var entity = await _dbSet.AsQueryable().FirstOrDefaultAsync(p => p.Id == id, ct);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task<bool> TryReduceStockAsync(int productId, decimal quantity)
        {
            // Use EF Core ExecuteUpdateAsync to perform atomic check-and-decrement while preserving query filters
            if (!_context.TenantCompanyId.HasValue) throw new InvalidOperationException("Tenant company id missing for multi-tenant operation.");
            var companyId = _context.TenantCompanyId.Value;

            var affected = await _context.Products
                .Where(p => p.Id == productId && EF.Property<int>(p, "CompanyId") == companyId && p.Stock >= quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - quantity));

            return affected > 0;
        }

        public async Task<bool> TryReserveStockAsync(int productId, decimal quantity)
        {
            // Increase ReservedStock only if Stock - ReservedStock >= quantity
            if (!_context.TenantCompanyId.HasValue) throw new InvalidOperationException("Tenant company id missing for multi-tenant operation.");
            var companyId = _context.TenantCompanyId.Value;

            var affected = await _context.Products
                .Where(p => p.Id == productId && EF.Property<int>(p, "CompanyId") == companyId && (p.Stock - p.ReservedStock) >= quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.ReservedStock, p => p.ReservedStock + quantity));

            return affected > 0;
        }

        public async Task ReleaseReservedStockAsync(int productId, decimal quantity)
        {
            if (!_context.TenantCompanyId.HasValue) throw new InvalidOperationException("Tenant company id missing for multi-tenant operation.");
            var companyId = _context.TenantCompanyId.Value;

            await _context.Products
                .Where(p => p.Id == productId && EF.Property<int>(p, "CompanyId") == companyId && p.ReservedStock >= quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.ReservedStock, p => p.ReservedStock - quantity));
        }

        // CRUD operations (Add/Update/Remove/GetById/List) are provided by GenericRepository
    }
}
