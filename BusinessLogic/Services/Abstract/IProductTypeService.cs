using System.Collections.Generic;
using Onion.Domain.Products;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IProductTypeService
    {
        Task<IEnumerable<ProductType>> GetAllAsync();
        Task<ProductType?> GetByIdAsync(int id);
        Task<ProductType> CreateAsync(ProductType entity);
        Task UpdateAsync(ProductType entity);
        Task DeleteAsync(int id);
    }
}
