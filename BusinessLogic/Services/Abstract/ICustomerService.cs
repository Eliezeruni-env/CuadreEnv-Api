using Onion.Domain;
using Onion.BussinesLogic.Models;
using System.Collections.Generic;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface ICustomerService
    {
        Task<IEnumerable<Customer>> GetAllAsync();
        Task<PagedResult<Customer>> GetPagedAsync(int pageNumber, int pageSize, string? search);
        Task<Customer?> GetByIdAsync(int id);
        Task<Customer> CreateAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task DeleteAsync(int id);
        Task<IEnumerable<Customer>> GetActiveCustomersAsync(int days);
    }
}
