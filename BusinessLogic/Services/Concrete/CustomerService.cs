using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain;
using Onion.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using Onion.DataAccess.Repositories.Concrete;
using Onion.BussinesLogic.Models;
using Onion.DataAccess.Repositories.Concrete;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _uow;
        private readonly Onion.BussinesLogic.Services.Abstract.IPaginationService _paginationService;

        public CustomerService(IUnitOfWork uow, Onion.BussinesLogic.Services.Abstract.IPaginationService paginationService)
        {
            _uow = uow;
            _paginationService = paginationService;
        }

        public async Task<Customer> CreateAsync(Customer customer)
        {
            if (customer is null) throw new ArgumentNullException(nameof(customer));
            if (string.IsNullOrWhiteSpace(customer.Name)) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_NAME", Message = "Customer name required", Language = "EN" });

            await _uow.Customers.AddAsync(customer);
            await _uow.SaveChangesAsync();
            return customer;
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _uow.Customers.GetByIdAsync(id) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Customer not found", Language = "EN" });
            _uow.Customers.Remove(existing);
            await _uow.SaveChangesAsync();
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            return await _uow.Customers.ListAsync();
        }

        public async Task<Onion.Common.Models.Pagination.PagedList<Customer>> GetPagedAsync(int pageNumber, int pageSize, string? search)
        {
            var query = (await _uow.Customers.ListAsync()).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLowerInvariant();
                query = query.Where(c => (c.Name ?? string.Empty).ToLower().Contains(s) || (c.Email ?? string.Empty).ToLower().Contains(s) || (c.Identification ?? string.Empty).ToLower().Contains(s));
            }

            var pn = Math.Max(1, pageNumber);
            var ps = Math.Clamp(pageSize, 1, 100);
            return await _paginationService.ToPagedListAsync(query, pn, ps);
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _uow.Customers.GetByIdAsync(id);
        }

        public async Task UpdateAsync(Customer customer)
        {
            if (customer is null) throw new ArgumentNullException(nameof(customer));
            var existing = await _uow.Customers.GetByIdAsync(customer.Id) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Customer not found", Language = "EN" });

            existing.Name = customer.Name;
            existing.Phone = customer.Phone;
            existing.Email = customer.Email;
            existing.Address = customer.Address;
            existing.Identification = customer.Identification;
            existing.Notes = customer.Notes;
            existing.Address = customer.Address;
            existing.Identification = customer.Identification;
            existing.Notes = customer.Notes;

            _uow.Customers.Update(existing);
            await _uow.SaveChangesAsync();
        }

        public async Task<IEnumerable<Customer>> GetActiveCustomersAsync(int days)
        {
            if (days <= 0) days = 30;
            var cutoff = DateTime.UtcNow.AddDays(-days);
            // Use UnitOfWork sales repository to find customers with sales in timeframe.
            var sales = await _uow.Sales.FindAsync(s => s.Date >= cutoff);
            var ids = sales.Where(s => s.CustomerId.HasValue).Select(s => s.CustomerId!.Value).Distinct().ToList();
            if (ids.Count == 0) return new List<Customer>();
            var customers = new List<Customer>();
            foreach (var id in ids)
            {
                var c = await _uow.Customers.GetByIdAsync(id);
                if (c != null) customers.Add(c);
            }
            return customers;
        }
    }
}
