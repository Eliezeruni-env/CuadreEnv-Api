    using Onion.DataAccess.Models;
using Onion.Common.Models.Pagination;
using Onion.Domain.Products;

namespace Onion.DataAccess.Repositories.Abstract
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<bool> ExistsByBarcodeAsync(string barcode, int id);
        Task<IEnumerable<Product>> SearchByDescriptionAsync(string description);
        Task<IEnumerable<Product>> SearchByNameAsync(string name);
        Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);
        Task<Product?> GetWithCategoryAsync(int id);
        Task<bool> ExistsByNameAsync(string name);
        Task<IEnumerable<Product>> GetLowStockAsync(int threshold);

        Task<PagedList<ProductRow>> GetPagedProductsAsync(FilterPayload filterPayload, CancellationToken ct = default);

        Task<IEnumerable<Product>> GetProductsBySqlAsync(decimal minCost);
        Task<Product?> GetByIdSqlAsync(int id);
        Task<int> UpdateCostBySqlAsync(int id, decimal newCost);
        Task UpdateAsync(Product entity, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
        // Try to reduce stock atomically; returns true if stock was reduced, false if insufficient stock
        Task<bool> TryReduceStockAsync(int productId, decimal quantity);
        Task<bool> TryCommitReservedStockAsync(int productId, decimal quantity);
        // Reserve stock atomically (increase ReservedStock and ensure Stock - ReservedStock >= 0)
        Task<bool> TryReserveStockAsync(int productId, decimal quantity);
        // Release reserved stock (decrease ReservedStock)
        Task ReleaseReservedStockAsync(int productId, decimal quantity);
        // Increase stock (e.g., on sale cancellation or purchase)
        Task<bool> TryIncreaseStockAsync(int productId, decimal quantity);
    }
}