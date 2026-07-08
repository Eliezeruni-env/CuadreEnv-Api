using System.Collections.Generic;
using Onion.Domain;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface ISupplierService
    {
        Task<IEnumerable<Supplier>> GetAllAsync();
        Task<Supplier?> GetByIdAsync(int id);
        Task<Supplier> CreateAsync(Supplier supplier);
        Task UpdateAsync(Supplier supplier);
        Task DeleteAsync(int id);
    }
}
