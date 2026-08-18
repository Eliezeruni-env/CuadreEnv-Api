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
        private readonly Onion.BussinesLogic.Services.Abstract.ISubscriptionService _subscriptionService;
        private readonly Onion.Common.Services.ICurrentUserService _currentUserService;

        public UserService(IUnitOfWork uow, ITenantProvider tenantProvider, Onion.BussinesLogic.Services.Abstract.ISubscriptionService subscriptionService, Onion.Common.Services.ICurrentUserService currentUserService)
        {
            _uow = uow;
            _tenantProvider = tenantProvider;
            _subscriptionService = subscriptionService;
            _currentUserService = currentUserService;
        }

        public async Task<User> CreateAsync(User user)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(user.Email)) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_EMAIL", Message = "Email required", Language = "EN" });
            if (await _uow.Users.ExistsByEmailAsync(user.Email)) throw new CustomException(new Onion.Common.Models.Error { Code = "DUPLICATE_EMAIL", Message = "Email already exists", Language = "EN" });

            // Check subscription limits before creating
            var companyId = user.CompanyId;
            try
            {
                var subscriptionService = (Onion.BussinesLogic.Services.Abstract.ISubscriptionService?)typeof(UserService).Assembly
                    .CreateInstance("Onion.BussinesLogic.Services.Concrete.SubscriptionService");
            }
            catch { }

            // Enforce plan limits if company is provided
            if (user.CompanyId.HasValue && user.CompanyId.Value > 0)
            {
                var canCreate = await _subscriptionService.CanCreateUserAsync(user.CompanyId.Value);
                if (!canCreate)
                    throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "PLAN_LIMIT", Message = "User limit reached for current subscription plan", Language = "EN" });
            }

            await _uow.Users.AddAsync(user);
            await _uow.SaveChangesAsync();
            user.PasswordHash = string.Empty;
            return user;
        }

        public async Task AssignCompanyAsync(int userId, int companyId)
        {
            // Determine caller context
            var callerCompany = _tenantProvider.GetCompanyId();
            var callerUserId = _currentUserService?.UserId;

            if (!callerCompany.HasValue)
            {
                // No tenant claim: allow only self-assignment (onboarding flow)
                if (!callerUserId.HasValue || callerUserId.Value != userId)
                    throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "FORBIDDEN", Message = "Tenant context missing: CompanyId claim required or caller must be the same user for onboarding", Language = "EN" });
            }
            else
            {
                // Tenant-scoped caller may only assign within their own company
                if (callerCompany.Value != companyId)
                    throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "FORBIDDEN", Message = "Cannot assign user to a different company", Language = "EN" });
            }

            var user = await _uow.Users.GetByIdAsync(userId) ?? throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "User not found", Language = "EN" });

            // If the user already has a different company assigned, refuse
            if (user.CompanyId.HasValue && user.CompanyId.Value != companyId)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "FORBIDDEN", Message = "Cannot assign user to a different company", Language = "EN" });

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

        public async Task SetRoleAsync(int userId, string role)
        {
            // Only allow when caller has tenant and user belongs to same company
            var callerCompany = _tenantProvider.GetCompanyId();
            if (!callerCompany.HasValue)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "FORBIDDEN", Message = "Tenant context missing: CompanyId claim required", Language = "EN" });

            var user = await _uow.Users.GetByIdAsync(userId) ?? throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "User not found", Language = "EN" });
            if (user.CompanyId != callerCompany.Value)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "FORBIDDEN", Message = "Cannot modify user from another company", Language = "EN" });

            user.Role = role;
            _uow.Users.Update(user);
            await _uow.SaveChangesAsync();
        }

        public async Task SetActiveAsync(int userId, bool active)
        {
            var callerCompany = _tenantProvider.GetCompanyId();
            if (!callerCompany.HasValue)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "FORBIDDEN", Message = "Tenant context missing: CompanyId claim required", Language = "EN" });

            var user = await _uow.Users.GetByIdAsync(userId) ?? throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "User not found", Language = "EN" });
            if (user.CompanyId != callerCompany.Value)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "FORBIDDEN", Message = "Cannot modify user from another company", Language = "EN" });

            // Soft deactivate/reactivate via Active flag on BaseEntity
            var prop = user.GetType().GetProperty("Active");
            if (prop != null)
            {
                prop.SetValue(user, active);
                _uow.Users.Update(user);
                await _uow.SaveChangesAsync();
            }
            else
            {
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "INTERNAL_ERROR", Message = "User entity does not support Active flag", Language = "EN" });
            }
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
