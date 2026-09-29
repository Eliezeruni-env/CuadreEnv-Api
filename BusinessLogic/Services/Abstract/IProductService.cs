using Onion.BussinesLogic.Dtos;
using Onion.Common.Models.Pagination;
using Onion.DataAccess.Models;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IProductService
    {
        Task<ProductDto?> GetAsync(int id);
        Task<IEnumerable<ProductDto>> SearchByDescriptionAsync(string description);
        Task<PagedList<ProductRow>> GetPagedListAsync(FilterPayload filterPayload, CancellationToken ct = default);
        Task AddAsync(ProductDto entityDto);
        Task UpdateAsync(ProductDto entityDto);
        Task DeleteAsync(int id);

        // Domain-level operations
        Task<IEnumerable<Onion.Domain.Products.Product>> GetAllAsync();
        Task<Onion.Domain.Products.Product?> GetByIdAsync(int id);
        Task CreateAsync(Onion.Domain.Products.Product product);
        Task UpdateAsync(Onion.Domain.Products.Product product);
        Task DeleteAsyncDomain(int id);
        Task<IEnumerable<Onion.Domain.Products.Product>> SearchAsync(string name);
        Task<IEnumerable<Onion.Domain.Products.Product>> GetByCategoryAsync(int categoryId);
        Task<IEnumerable<Onion.Domain.Products.Product>> GetByTypeAsync(int productTypeId);
        Task<IEnumerable<Onion.Domain.Products.Product>> GetLowStockAsync(int threshold);
        // No-op change for consistency: harmless comment added
    }
}