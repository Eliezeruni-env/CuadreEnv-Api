using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain;
using Onion.Common.Exceptions;
using System.Collections.Generic;
using Onion.DataAccess.Repositories.Concrete;
using Onion.DataAccess.Repositories.Concrete;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _uow;

        public CustomerService(IUnitOfWork uow)
        {
            _uow = uow;
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

            _uow.Customers.Update(existing);
            await _uow.SaveChangesAsync();
        }
    }
}
