using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain;

namespace Onion.DataAccess.Repositories.Concrete
{
    public class PurchaseRepository : GenericRepository<Purchase>, IPurchaseRepository
    {
        public PurchaseRepository(OnionDbContext context) : base(context) { }

        public async Task<Purchase?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Purchases.Include(p => p.Details).FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<System.Collections.Generic.List<Purchase>> GetAllWithDetailsAsync()
        {
            return await _context.Purchases.AsNoTracking().Include(p => p.Details).OrderByDescending(p => p.Id).ToListAsync();
        }
    }
}
