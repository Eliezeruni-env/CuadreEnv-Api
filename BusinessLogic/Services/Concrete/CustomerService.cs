using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Abstract;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain;
using Onion.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using Onion.BussinesLogic.Models;
using Microsoft.EntityFrameworkCore;
using Onion.DataAccess;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _uow;
        private readonly Onion.BussinesLogic.Services.Abstract.IPaginationService _paginationService;
        private readonly ITenantProvider _tenantProvider;

        public CustomerService(IUnitOfWork uow, Onion.BussinesLogic.Services.Abstract.IPaginationService paginationService, ITenantProvider tenantProvider)
        {
            _uow = uow;
            _paginationService = paginationService;
            _tenantProvider = tenantProvider;
        }

        public async Task<Customer> CreateAsync(Customer customer)
        {
            if (customer is null) throw new ArgumentNullException(nameof(customer));
            if (string.IsNullOrWhiteSpace(customer.Name)) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_NAME", Message = "Customer name required", Language = "EN" });

            var companyId = _tenantProvider.GetCompanyId();
            if (companyId is not > 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "COMPANY_REQUIRED", Message = "Authenticated company is required", Language = "EN" });
            customer.CompanyId = companyId.Value;

            await _uow.Customers.AddAsync(customer);
            await _uow.SaveChangesAsync();
            return customer;
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _uow.Customers.GetByIdAsync(id) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Customer not found", Language = "EN" });
            if (existing.IsGeneric)
                throw new CustomException(new Onion.Common.Models.Error { Code = "GENERIC_CUSTOMER_IMMUTABLE", Message = "The contado customer cannot be deleted", Language = "EN" });
            _uow.Customers.Remove(existing);
            await _uow.SaveChangesAsync();
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            return await _uow.Customers.ListAsync();
        }

        public async Task<Onion.Common.Models.Pagination.PagedList<Customer>> GetPagedAsync(int pageNumber, int pageSize, string? search)
        {
            await EnsureGenericCustomerAsync();
            var query = _uow.Customers.Query();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLowerInvariant();
                query = query.Where(c => (c.Name ?? string.Empty).ToLower().Contains(s) || (c.Email ?? string.Empty).ToLower().Contains(s) || (c.Identification ?? string.Empty).ToLower().Contains(s));
            }

            var pn = Math.Max(1, pageNumber);
            var ps = Math.Clamp(pageSize, 1, 1000);
            return await _paginationService.ToPagedListAsync(query, pn, ps);
        }

        private async Task EnsureGenericCustomerAsync()
        {
            var companyId = _tenantProvider.GetCompanyId();

            if (!companyId.HasValue || companyId.Value <= 0)
                return;

            var exists = (await _uow.Customers.FindAsync(c => c.CompanyId == companyId.Value && c.IsGeneric)).Any();
            if (exists)
                return;

            await _uow.Customers.AddAsync(new Customer
            {
                CompanyId = companyId.Value,
                Name = "Cliente Contado",
                Identification = "000-0000000-0",
                Phone = null,
                Email = null,
                Active = true,
                IsGeneric = true
            });
            await _uow.SaveChangesAsync();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _uow.Customers.GetByIdAsync(id);
        }

        public async Task UpdateAsync(Customer customer)
        {
            if (customer is null) throw new ArgumentNullException(nameof(customer));
            var existing = await _uow.Customers.GetByIdAsync(customer.Id) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Customer not found", Language = "EN" });

            if (existing.IsGeneric)
                throw new CustomException(new Onion.Common.Models.Error { Code = "GENERIC_CUSTOMER_IMMUTABLE", Message = "The contado customer cannot be modified", Language = "EN" });

            existing.Name = customer.Name;
            existing.Phone = customer.Phone;
            existing.Email = customer.Email;
            existing.Address = customer.Address;
            existing.Identification = customer.Identification;
            existing.Rnc = customer.Rnc;
            existing.Notes = customer.Notes;
            existing.Active = customer.Active;

            _uow.Customers.Update(existing);
            await _uow.SaveChangesAsync();
        }

        public async Task<IEnumerable<Customer>> GetActiveCustomersAsync(int days)
        {
            if (days <= 0) days = 30;
            var cutoff = DateTime.UtcNow.AddDays(-days);
            var ids = await _uow.Sales.Query()
                .AsNoTracking()
                .Where(s => s.Date >= cutoff && s.CustomerId.HasValue)
                .Select(s => s.CustomerId!.Value)
                .Distinct()
                .ToListAsync();
            if (ids.Count == 0) return new List<Customer>();
            return await _uow.Customers.Query()
                .AsNoTracking()
                .Where(c => ids.Contains(c.Id))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}
