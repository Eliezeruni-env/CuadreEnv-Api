using System.Collections.Generic;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IGenericService<T>
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(int id);
        Task<T> CreateAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
    }
}
