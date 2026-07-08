using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain.Users;
using Onion.Common.Exceptions;
using System.Collections.Generic;
using Onion.DataAccess.Repositories.Concrete;

namespace Onion.BussinesLogic.Services.Concrete
{
    using Onion.DataAccess;

    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;
        private readonly ITenantProvider _tenantProvider;

        public UserService(IUnitOfWork uow, ITenantProvider tenantProvider)
        {
            _uow = uow;
            _tenantProvider = tenantProvider;
        }

        public async Task<User> CreateAsync(User user)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(user.Email)) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_EMAIL", Message = "Email required", Language = "EN" });
            if (await _uow.Users.ExistsByEmailAsync(user.Email)) throw new CustomException(new Onion.Common.Models.Error { Code = "DUPLICATE_EMAIL", Message = "Email already exists", Language = "EN" });

            await _uow.Users.AddAsync(user);
            await _uow.SaveChangesAsync();
            user.PasswordHash = string.Empty;
            return user;
        }

        public async Task AssignCompanyAsync(int userId, int companyId)
        {
            // Require tenant-scoped caller. Do not allow operations when caller has no CompanyId claim.
            var callerCompany = _tenantProvider.GetCompanyId();
            if (!callerCompany.HasValue)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "FORBIDDEN", Message = "Tenant context missing: CompanyId claim required", Language = "EN" });

            // Prevent a tenant-scoped caller from assigning a user to a different company
            if (callerCompany.Value != companyId)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "FORBIDDEN", Message = "Cannot assign user to a different company", Language = "EN" });

            var user = await _uow.Users.GetByIdAsync(userId) ?? throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "User not found", Language = "EN" });
            user.CompanyId = companyId;
            _uow.Users.Update(user);
            await _uow.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _uow.Users.GetByIdAsync(id) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "User not found", Language = "EN" });
            _uow.Users.Remove(existing);
            await _uow.SaveChangesAsync();
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            // Explicitly scope user listing to the current tenant to avoid leaking users across companies.
            var companyId = _tenantProvider.GetCompanyId();
            if (!companyId.HasValue)
                return new List<User>();

            return await _uow.Users.FindAsync(u => u.CompanyId == companyId.Value);
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _uow.Users.GetByIdAsync(id);
        }

        public async Task UpdateAsync(User user)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            var existing = await _uow.Users.GetByIdAsync(user.Id) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "User not found", Language = "EN" });

            existing.FirstName = user.FirstName;
            existing.LastName = user.LastName;
            existing.PhoneNumber = user.PhoneNumber;
            existing.UserName = user.UserName;

            _uow.Users.Update(existing);
            await _uow.SaveChangesAsync();
        }
    }
}
