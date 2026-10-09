using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain;

namespace Onion.DataAccess.Repositories.Concrete
{
    public class SaleRepository : GenericRepository<Sale>, ISaleRepository
    {
        public SaleRepository(OnionDbContext context) : base(context) { }

        public async Task<Sale?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Sales.Include(s => s.Details).FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Sale?> GetByIdIgnoreQueryFiltersAsync(int id)
        {
            return await _context.Sales.IgnoreQueryFilters().Include(s => s.Details).FirstOrDefaultAsync(s => s.Id == id);
        }
    }
}
