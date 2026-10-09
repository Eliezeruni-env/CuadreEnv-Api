using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Onion.Common.Models.Pagination;

namespace Onion.DataAccess.Repositories.Abstract
{
    public interface IRepository<T> where T : Onion.Domain.BaseEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> ListAsync();
        IQueryable<T> Query() => throw new NotSupportedException("This repository does not expose a query source.");
        // Return a paged list of entities. PageNumber starts at 1.
        Task<Onion.Common.Models.Pagination.PagedList<T>> GetPagedAsync(int pageNumber, int pageSize);
        // Return a paged list from a pre-filtered query (caller can apply filters before paging)
        Task<PagedList<T>> GetPagedAsync(IQueryable<T> query, int pageNumber, int pageSize);
        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
    }
}
