using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain.Users;

namespace Onion.DataAccess.Repositories.Concrete
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(OnionDbContext context) : base(context) { }

        public async Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var normalized = email.Trim().ToLowerInvariant();
            // Use raw SQL to avoid translation issues with certain database collations/providers
            return await _context.Set<User>().FromSqlInterpolated<User>($"SELECT * FROM [Users] WHERE [Email] = {normalized}").IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync();
        }

        public async Task<User?> GetByIdUnscopedAsync(int id)
        {
            return await _context.Set<User>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public void UpdateLoginMetadata(User user)
        {
            // Attach the entity and mark only login-related properties as modified so
            // tenant-scoped fields are not affected during unauthenticated login flows.
            _context.Set<User>().Attach(user);
            _context.Entry(user).Property(x => x.LastLoginAt).IsModified = true;
            _context.Entry(user).Property(x => x.LastLoginIp).IsModified = true;
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var normalized = email.Trim().ToLowerInvariant();
            var found = await _context.Set<User>().FromSqlInterpolated<User>($"SELECT * FROM [Users] WHERE [Email] = {normalized}").IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync();
            return found != null;
        }

        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password)) return null;
            var user = await GetByEmailAsync(email);
            if (user == null) return null;

            // Legacy SHA256 hex verification (old users)
            using var sha = System.Security.Cryptography.SHA256.Create();
            var pwdBytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(pwdBytes);
            var hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            if (string.Equals(user.PasswordHash ?? string.Empty, hex, StringComparison.OrdinalIgnoreCase))
                return user;

            return null;
        }
    }
}
