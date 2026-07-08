using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Onion.DataAccess.Repositories.Abstract
{
    public interface IRepository<T> where T : Onion.Domain.BaseEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> ListAsync();
        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
    }
}
