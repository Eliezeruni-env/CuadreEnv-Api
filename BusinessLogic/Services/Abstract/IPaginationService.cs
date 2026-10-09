using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Onion.Common.Models.Pagination;
using Onion.DataAccess.Models;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IPaginationService
    {
        Task<PagedList<T>> ToPagedListAsync<T>(IQueryable<T> query, int pageNumber, int pageSize, CancellationToken ct = default);
        Task<PagedList<T>> ToPagedListAsync<T>(IQueryable<T> query, FilterPayload filterPayload, CancellationToken ct = default);
    }
}
