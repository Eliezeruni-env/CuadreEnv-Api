using System.Collections.Generic;
using System.Threading.Tasks;
using Onion.Domain;

namespace Onion.DataAccess.Repositories.Abstract
{
    public interface ISaleRepository : IRepository<Sale>
    {
        Task<Sale?> GetByIdWithDetailsAsync(int id);
        Task<Sale?> GetByIdIgnoreQueryFiltersAsync(int id);
        Task<List<Sale>> ListWithDetailsAsync();
        Task<Onion.Common.Models.Pagination.PagedList<Sale>> GetPagedWithDetailsAsync(int pageNumber, int pageSize);
    }
}
