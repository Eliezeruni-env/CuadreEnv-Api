using Onion.Domain;
using System.Collections.Generic;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface ICustomerService
    {
        Task<IEnumerable<Customer>> GetAllAsync();
        Task<Customer?> GetByIdAsync(int id);
        Task<Customer> CreateAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task DeleteAsync(int id);
    }
}
