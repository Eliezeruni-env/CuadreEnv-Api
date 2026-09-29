using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Models.Pagination;
using Onion.DataAccess.Extensions;
using Onion.DataAccess.Models;

namespace Onion.BussinesLogic.Services.Infrastructure
{
    public class PaginationService : IPaginationService
    {
        public Task<PagedList<T>> ToPagedListAsync<T>(IQueryable<T> query, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            return query.ToPagedListAsync(pageNumber, pageSize, ct);
        }

        public Task<PagedList<T>> ToPagedListAsync<T>(IQueryable<T> query, FilterPayload filterPayload, CancellationToken ct = default)
        {
            var pn = filterPayload?.PageNumber ?? 1;
            var ps = filterPayload?.PageSize ?? 10;
            return query.ToPagedListAsync(pn, ps, ct);
        }
    }
}
