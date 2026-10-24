using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain;
using Onion.DataAccess.Extensions;

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

        public async Task<List<Sale>> ListWithDetailsAsync()
        {
            return await _context.Sales.Include(s => s.Details).OrderByDescending(s => s.Date).ToListAsync();
        }

        public async Task<Onion.Common.Models.Pagination.PagedList<Sale>> GetPagedWithDetailsAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            var query = _context.Sales.Include(s => s.Details).OrderByDescending(s => s.Date);
            return await query.ToPagedListAsync(pageNumber, pageSize);
        }
    }
}
