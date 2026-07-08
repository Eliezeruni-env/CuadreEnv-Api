using Onion.Domain;
using System.Collections.Generic;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface ICompanyService
    {
        Task<IEnumerable<Company>> GetAllAsync();
        Task<Company?> GetByIdAsync(int id);
        // Create using full entity (legacy/advanced callers)
        Task<Company> CreateAsync(Company company);
        // Create using a clean DTO for controller/request usage
        Task<Company> CreateAsync(Onion.BussinesLogic.Dtos.CreateCompanyRequest request);
        Task UpdateAsync(Company company);
        Task DeleteAsync(int id);
    }
}
