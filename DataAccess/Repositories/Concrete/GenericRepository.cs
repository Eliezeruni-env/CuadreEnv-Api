using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Onion.DataAccess.Repositories.Abstract;

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
            // First, check existence ignoring query filters to determine if resource belongs to another company
            var withoutFilter = await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id);
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

        public async Task<IEnumerable<T>> ListAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsQueryable().Where(predicate).ToListAsync();
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
