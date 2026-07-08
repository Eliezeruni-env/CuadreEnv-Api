using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Onion.DataAccess.Models;
using Onion.Common.Models.Pagination;

namespace Onion.DataAccess.Repositories.Concrete
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(OnionDbContext context) : base(context)
        {
        }

        public Task<bool> ExistsByDescriptionAsync(string descripition, int id)
        {
            if (string.IsNullOrWhiteSpace(descripition))
                return Task.FromResult(false);

            var query = _context.Categories.AsQueryable()
                .Where(x => x.Description == descripition);

            if (id > 0)
                query = query.Where(x => x.Id != id);

            return query.AnyAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsAsync(int categoryId)
        {
            return await _context.Products.Where(p => p.CategoryId == categoryId).ToListAsync();
        }

        public async Task<IEnumerable<Category>> GetWithProductsAsync()
        {
            // Category has no navigation property to Products; return categories and consumers can query products as needed.
            return await _context.Categories.ToListAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var n = name.Trim();
            return await _context.Categories.AnyAsync(c => c.Description == n);
        }
    }
}
