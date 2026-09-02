using System.Threading.Tasks;
using Onion.Domain;

namespace Onion.DataAccess.Repositories.Abstract
{
    public interface ISaleRepository : IRepository<Sale>
    {
        Task<Sale?> GetByIdWithDetailsAsync(int id);
        // Find by id ignoring global query filters (useful to detect tenant ownership)
        Task<Sale?> GetByIdIgnoreQueryFiltersAsync(int id);
    }
}
