using Onion.Domain.Users;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User> CreateAsync(User user);
        Task AssignCompanyAsync(int userId, int companyId);
        Task UpdateAsync(User user);
        Task DeleteAsync(int id);
    }
}
