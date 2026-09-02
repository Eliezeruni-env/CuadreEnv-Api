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
            try
            {
                // First, check existence ignoring query filters to determine if resource belongs to another company
                var withoutFilter = await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id);

                if (withoutFilter == null) return null;
            if (withoutFilter == null) return null;

            // If entity exposes CompanyId and tenant is set, enforce explicit 403 when mismatched
            var companyProp = withoutFilter.GetType().GetProperty("CompanyId");
                if (companyProp != null && _context.TenantCompanyId.HasValue)
                {
                    var entityCompany = (int)companyProp.GetValue(withoutFilter)!;
                    if (entityCompany != _context.TenantCompanyId.Value)
                        throw new CustomException(new Error { Code = "FORBIDDEN", Message = "Access to resource from another company is forbidden", Language = "EN" });
                }

                // Return entity respecting global query filters (tenant scoping) to avoid accidental exposure
                // Include common navigations by convention using EF Core metadata so services don't need to load them.
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
                    // If metadata inspection fails for any reason, fall back to returning the entity without includes.
                }

                return await query.FirstOrDefaultAsync(e => e.Id == id);
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208)
            {
                // Table does not exist in database (missing migration) -- return null so callers handle gracefully
                return null;
            }
        }

        public async Task<IEnumerable<T>> ListAsync()
        {
            try
            {
                return await _dbSet.ToListAsync();
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208)
            {
                // Table missing - return empty list so UI can function until migration is applied
                return new List<T>();
            }
        }

        public async Task<Onion.Common.Models.Pagination.PagedList<T>> GetPagedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            try
            {
                var query = _dbSet.AsQueryable();
                var total = await query.CountAsync();
                var pageCount = (int)Math.Ceiling(total / (double)pageSize);
                var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
                return new Onion.Common.Models.Pagination.PagedList<T>(items, pageSize, pageCount, total);
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208)
            {
                // Table missing - return empty paged result so frontend can function
                var items = new List<T>();
                return new Onion.Common.Models.Pagination.PagedList<T>(items, pageSize, 0, 0);
            }
        }

        // Overload to produce paged result from an existing query (allows callers to apply filters before paging)
        public Task<Onion.Common.Models.Pagination.PagedList<T>> GetPagedAsync(IQueryable<T> query, int pageNumber, int pageSize)
        {
            // Delegate to PaginationExtensions to keep consistent behavior
            return query.ToPagedListAsync(pageNumber, pageSize);
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return await _dbSet.AsQueryable().Where(predicate).ToListAsync();
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208)
            {
                return new List<T>();
            }
        }

        public void Remove(T entity)
        {
            // Ensure tenant ownership when entity exposes CompanyId
            var companyProp = entity.GetType().GetProperty("CompanyId");
            if (companyProp != null && _context.TenantCompanyId.HasValue)
            {
                var entityCompany = (int)companyProp.GetValue(entity)!;
                if (entityCompany != _context.TenantCompanyId.Value)
                    throw new CustomException(new Error { Code = "FORBIDDEN", Message = "Access to resource from another company is forbidden", Language = "EN" });
            }

            _dbSet.Remove(entity);
        }

        public void Update(T entity)
        {
            var companyProp = entity.GetType().GetProperty("CompanyId");
            if (companyProp != null && _context.TenantCompanyId.HasValue)
            {
                var entityCompany = (int)companyProp.GetValue(entity)!;
                if (entityCompany != _context.TenantCompanyId.Value)
                    throw new CustomException(new Error { Code = "FORBIDDEN", Message = "Access to resource from another company is forbidden", Language = "EN" });
            }

            _dbSet.Update(entity);
        }
    }
}
