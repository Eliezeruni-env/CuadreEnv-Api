using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Onion.DataAccess.Repositories.Abstract;
using Onion.DataAccess.Extensions;

namespace Onion.DataAccess.Repositories.Concrete
{
    using Onion.Common.Exceptions;
    using Onion.Common.Models;

    public class GenericRepository<T> : IRepository<T> where T : Onion.Domain.BaseEntity
    {
        protected readonly OnionDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(OnionDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            var query = _dbSet.AsQueryable();
            try
            {
                var entityType = _context.Model.FindEntityType(typeof(T));
                if (entityType != null)
                {
                    var navigations = entityType.GetNavigations();
                    foreach (var nav in navigations)
                    {
                        // include navigation by name
                        query = query.Include(nav.Name);
                    }
                }
            }
            catch
            {
                // If metadata inspection fails, query the entity without inferred includes.
            }

            return await query.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IEnumerable<T>> ListAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public IQueryable<T> Query() => _dbSet.AsQueryable();

        public async Task<Onion.Common.Models.Pagination.PagedList<T>> GetPagedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            var query = _dbSet.AsQueryable();
            var total = await query.CountAsync();
            var pageCount = (int)Math.Ceiling(total / (double)pageSize);
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new Onion.Common.Models.Pagination.PagedList<T>(items, pageSize, pageCount, total);
        }

        // Overload to produce paged result from an existing query (allows callers to apply filters before paging)
        public Task<Onion.Common.Models.Pagination.PagedList<T>> GetPagedAsync(IQueryable<T> query, int pageNumber, int pageSize)
        {
            // Delegate to PaginationExtensions to keep consistent behavior
            return query.ToPagedListAsync(pageNumber, pageSize);
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsQueryable().Where(predicate).ToListAsync();
        }

        public void Remove(T entity)
        {
            // Ensure tenant ownership when entity exposes CompanyIde
            var companyProp = entity.GetType().GetProperty("CompanyId");
            if (companyProp != null && !HasGlobalTenantBypass())
            {
                if (!_context.TenantCompanyId.HasValue || !TenantOwnsEntity(companyProp, entity))
                    throw new CustomException(new Error { Code = "FORBIDDEN", Message = "Access to resource from another company is forbidden", Language = "EN" });
            }

            _dbSet.Remove(entity);
        }

        public void Update(T entity)
        {
            var companyProp = entity.GetType().GetProperty("CompanyId");
            if (companyProp != null && !HasGlobalTenantBypass())
            {
                if (!_context.TenantCompanyId.HasValue || !TenantOwnsEntity(companyProp, entity))
                    throw new CustomException(new Error { Code = "FORBIDDEN", Message = "Access to resource from another company is forbidden", Language = "EN" });
            }

            _dbSet.Update(entity);
        }

        private bool HasGlobalTenantBypass() => _context.IsGlobalTenantAccess;

        private bool TenantOwnsEntity(System.Reflection.PropertyInfo companyProp, T entity)
        {
            var value = companyProp.GetValue(entity);
            return value is int entityCompany && entityCompany == _context.TenantCompanyId!.Value;
        }
    }
}
