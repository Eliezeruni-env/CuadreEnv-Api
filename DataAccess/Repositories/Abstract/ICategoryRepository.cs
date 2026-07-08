using Onion.Common.Models.Pagination;
using Onion.DataAccess.Models;
using Onion.Domain.Products;


namespace Onion.DataAccess.Repositories.Abstract
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<bool> ExistsByDescriptionAsync(string descripition, int id);
        Task<IEnumerable<Product>> GetProductsAsync(int categoryId);
        Task<IEnumerable<Category>> GetWithProductsAsync();
        Task<bool> ExistsByNameAsync(string name);
    }
}
