using System.Threading.Tasks;
using System;
using Onion.Domain.Users;

namespace Onion.DataAccess.Repositories.Abstract
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdUnscopedAsync(int id);
        Task<bool> ExistsByEmailAsync(string email);
        Task<User?> AuthenticateAsync(string email, string password);
        void UpdateLoginMetadata(User user);
    }
}
