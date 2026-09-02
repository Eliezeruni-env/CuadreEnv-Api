using System.Collections.Generic;
using Onion.Domain;
using Onion.Common.Models.Pagination;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface ISupplierService
    {
        Task<PagedList<Supplier>> GetPagedAsync(int pageNumber, int pageSize);
        Task<IEnumerable<Supplier>> GetAllAsync();
        Task<Supplier?> GetByIdAsync(int id);
        Task<Supplier> CreateAsync(Supplier supplier);
        Task UpdateAsync(Supplier supplier);
        Task DeleteAsync(int id);
    }
}
