using Microsoft.AspNetCore.Mvc;
using Onion.DataAccess.Repositories.Concrete;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain.Users;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;
using Onion.Common.Authorization;

namespace Onion.Controllers
{
    [Route("internal")]
    [ApiController]
    public class InternalUserController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly Onion.DataAccess.OnionDbContext _db;
        private readonly IUserService _userService;
        private readonly IAuthService _authService;

        public InternalUserController(IUnitOfWork uow, Onion.DataAccess.OnionDbContext db, IUserService userService, IAuthService authService)
        {
            _uow = uow;
            _db = db;
            _userService = userService;
            _authService = authService;
        }

        public record CreateUserRequest(
            string ExternalId,
            string Email,
            string FirstName,
            string LastName,
            string UserName,
            int? CompanyId,
            string? Role,
            string? Password
        );

        public record BlockUserRequest(
            string NewStatus,
            string ChangedBy,
            string Reason
        );

        public record RevokeSessionsRequest(
            string Reason,
            string PerformedBy
        );

        // 1b. List users (paged)
        [HttpGet("users")]
        public async Task<IActionResult> ListUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null, [FromQuery] bool? canLogin = null, [FromQuery] int? companyId = null)
        {
            var all = (await _uow.Users.ListAsync()).ToList();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                all = all.Where(u => (u.Email ?? string.Empty).ToLower().Contains(s) || (u.FirstName ?? string.Empty).ToLower().Contains(s) || (u.LastName ?? string.Empty).ToLower().Contains(s) || (u.Identification ?? string.Empty).ToLower().Contains(s)).ToList();
            }

            // Filter to only users that can log in if requested: Active && not deleted && have a password hash
            if (canLogin.HasValue && canLogin.Value)
            {
                all = all.Where(u => u.Active && !u.IsDeleted && !string.IsNullOrWhiteSpace(u.PasswordHash)).ToList();
            }

            if (companyId.HasValue)
            {
                all = all.Where(u => u.CompanyId == companyId.Value).ToList();
            }

            var total = all.Count;
            var items = all.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return Ok(new
            {
                items = items.Select(u => new
                {
                    saasUserId = u.Id,
                    externalId = u.Identification,
                    email = u.Email,
                    firstName = u.FirstName,
                    lastName = u.LastName,
                    companyId = u.CompanyId,
                    role = u.Role,
                    isSuperUser = u.IsSuperUser,
                    allowedModules = ModuleCodes.FromClaimPreservingIds(u.AllowedModulesJson),
                    accessStatus = (u.Active && !u.IsDeleted) ? "Active" : "Blocked"
                }),
                pageNumber,
                pageSize,
                total
            });
        }

        [HttpGet("users/{userIdStr}")]
        public async Task<IActionResult> GetUser(string userIdStr)
        {
            var user = await FindUserByIdOrExternalIdAsync(userIdStr);
            if (user == null) return NotFound();

            var subscriptions = await _uow.CompanySubscriptions.FindAsync(s => s.CompanyId == user.CompanyId);

            return Ok(new
            {
                saasUserId = user.Id,
                externalId = user.Identification,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                companyId = user.CompanyId,
                role = user.Role,
                isSuperUser = user.IsSuperUser,
                allowedModules = ModuleCodes.FromClaimPreservingIds(user.AllowedModulesJson),
                accessStatus = (user.Active && !user.IsDeleted) ? "Active" : "Blocked",
                subscriptions = subscriptions.Select(s => new {
                    subscriptionId = s.Id,
                    planId = s.SubscriptionPlanId,
                    startsAt = s.StartDate,
                    nextPaymentAt = s.RenewalDate,
                    status = s.Status.ToString()
                }),
                createdAt = user.CreationDate
            });
        }

        private async Task<User?> FindUserByIdOrExternalIdAsync(string userIdStr)
        {
            if (string.IsNullOrWhiteSpace(userIdStr)) return null;

            var normalizedId = userIdStr.Trim();

            // USM may send either the local SaaS id or the external id. Internal
            // calls are already protected by InternalApiAuthMiddleware, so do not
            // apply the current request tenant filter while resolving the user.
            // Prefer the external id first because it can also be numeric.
            var byExternalId = await _db.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Identification == normalizedId);
            if (byExternalId != null) return byExternalId;

            if (int.TryParse(normalizedId, out var id))
            {
                return await _db.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == id);
            }

            return null;
        }

        // 1. Create user
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req)
        {
            if (req == null) return BadRequest("Body required");

            var emailNormalized = req.Email.ToLower().Trim();

            // Set ambient tenant override
            Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = req.CompanyId;
            try
            {
                // Check if user already exists by externalId
                var existingByIdent = await FindUserByIdOrExternalIdAsync(req.ExternalId);
                if (existingByIdent != null)
                {
                    return Conflict(new { error = "Conflict", saasUserId = existingByIdent.Id });
                }

                // Check if email already exists
                var usersByEmail = await _uow.Users.FindAsync(u => u.Email == emailNormalized);
                var existingByEmail = usersByEmail.FirstOrDefault();
                if (existingByEmail != null)
                {
                    return Conflict(new { error = "Conflict", saasUserId = existingByEmail.Id });
                }

                if (req.CompanyId.HasValue)
                {
                    var company = (await _uow.Companies.FindAsync(c => c.Id == req.CompanyId.Value)).FirstOrDefault();
                    if (company == null)
                        return BadRequest(new { error = "Company does not exist." });
                }

                var newUser = new User
                {
                    Identification = req.ExternalId,
                    Email = emailNormalized,
                    FirstName = req.FirstName,
                    LastName = string.IsNullOrWhiteSpace(req.LastName) ? "." : req.LastName,
                    UserName = string.IsNullOrWhiteSpace(req.UserName) ? req.Email.Split('@')[0] : req.UserName,
                    Gender = "M", // Default required field
                    CompanyId = req.CompanyId,
                    Role = string.IsNullOrWhiteSpace(req.Role) ? "Employee" : req.Role,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password ?? "DefaultPassword123!"), // Hash with BCrypt
                    PhoneNumber = string.Empty
                };

                await _uow.Users.AddAsync(newUser);
                await _uow.SaveChangesAsync();

                return StatusCode(201, new { result = "ok", saasUserId = newUser.Id, requestId = Guid.NewGuid().ToString() });
            }
            finally
            {
                Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = null;
            }
        }

        // 2. Block/Unblock user
        [HttpPost("users/{userIdStr}/status")]
        public async Task<IActionResult> BlockUser(string userIdStr, [FromBody] BlockUserRequest req)
        {
            var user = await FindUserByIdOrExternalIdAsync(userIdStr);
            if (user == null) return NotFound();

            bool active = req.NewStatus == "Active";
            
            Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = user.CompanyId;
            try
            {
                // Soft deactivate/reactivate
                var prop = user.GetType().GetProperty("Active");
                if (prop != null)
                {
                    prop.SetValue(user, active);
                    _uow.Users.Update(user);
                    await _uow.SaveChangesAsync();
                }

                if (!active)
                {
                    await _authService.RevokeAllTokensAsync(user.Id);
                }

                return Ok(new { result = "ok", requestId = Guid.NewGuid().ToString() });
            }
            finally
            {
                Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = null;
            }
        }

        // 3. Revoke sessions
        [HttpPost("users/{userIdStr}/revoke-sessions")]
        public async Task<IActionResult> RevokeSessions(string userIdStr, [FromBody] RevokeSessionsRequest req)
        {
            var user = await FindUserByIdOrExternalIdAsync(userIdStr);
            if (user == null) return NotFound();

            await _authService.RevokeAllTokensAsync(user.Id);
            return Ok(new { result = "ok", requestId = Guid.NewGuid().ToString() });
        }

        // 4. Soft-delete user
        [HttpDelete("users/{userIdStr}")]
        public async Task<IActionResult> DeleteUser(string userIdStr)
        {
            var user = await FindUserByIdOrExternalIdAsync(userIdStr);
            if (user == null) return NotFound();

            Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = user.CompanyId;
            try
            {
                user.IsDeleted = true;
                
                var prop = user.GetType().GetProperty("Active");
                if (prop != null)
                {
                    prop.SetValue(user, false);
                }
                
                _uow.Users.Update(user);
                await _uow.SaveChangesAsync();

                await _authService.RevokeAllTokensAsync(user.Id);

                return Ok(new { result = "ok", requestId = Guid.NewGuid().ToString() });
            }
            finally
            {
                Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = null;
            }
        }

        // 5. Assign subscription for user's company
        [HttpPost("users/{userIdStr}/subscription")]
        public async Task<IActionResult> AssignSubscription(string userIdStr, [FromBody] CompanySubscriptionRequest req)
        {
            var user = await FindUserByIdOrExternalIdAsync(userIdStr);
            if (user == null) return NotFound();
            if (!user.CompanyId.HasValue) return BadRequest(new { error = "User has no companyId" });

            Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = user.CompanyId;
            try
            {
                var sub = new Onion.Domain.Billing.CompanySubscription
                {
                    CompanyId = user.CompanyId.Value,
                    SubscriptionPlanId = req.SubscriptionPlanId,
                    StartDate = req.StartsAt ?? DateTime.UtcNow,
                    RenewalDate = req.NextPaymentAt,
                    Status = Onion.Domain.Billing.SubscriptionStatus.Active
                };

                await _uow.CompanySubscriptions.AddAsync(sub);
                await _uow.SaveChangesAsync();

                return Ok(new { result = "ok", subscriptionId = sub.Id, requestId = Guid.NewGuid().ToString() });
            }
            finally
            {
                Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = null;
            }
        }

        // 6. Update next payment date for subscription
        [HttpPatch("subscriptions/{subscriptionId}/next-payment")]
        public async Task<IActionResult> UpdateNextPayment(int subscriptionId, [FromBody] NextPaymentRequest req)
        {
            var existing = await _uow.CompanySubscriptions.GetByIdAsync(subscriptionId);
            if (existing == null) return NotFound();

            Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = existing.CompanyId;
            try
            {
                existing.RenewalDate = req.NextPaymentAt;
                _uow.CompanySubscriptions.Update(existing);
                await _uow.SaveChangesAsync();
                return Ok(new { result = "ok", nextPaymentAt = existing.RenewalDate, requestId = Guid.NewGuid().ToString() });
            }
            finally
            {
                Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = null;
            }
        }

        public record CompanySubscriptionRequest(int SubscriptionPlanId, DateTime? StartsAt = null, DateTime? NextPaymentAt = null);
        public record NextPaymentRequest(DateTime NextPaymentAt);

        // 7. Assign subscription by plan code (UM can send plan code instead of numeric id)
        [HttpPost("users/{userIdStr}/subscription/by-plan-code")]
        public async Task<IActionResult> AssignSubscriptionByCode(string userIdStr, [FromBody] CompanySubscriptionByCodeRequest req)
        {
            var user = await FindUserByIdOrExternalIdAsync(userIdStr);
            if (user == null) return NotFound();
            if (!user.CompanyId.HasValue) return BadRequest(new { error = "User has no companyId" });

            if (string.IsNullOrWhiteSpace(req.PlanCode)) return BadRequest(new { error = "planCode required" });

            Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = user.CompanyId;
            try
            {
                var code = req.PlanCode.Trim().ToLowerInvariant();
                var plans = await _uow.SubscriptionPlans.FindAsync(p => p.Name.ToLower() == code);
                var plan = plans.FirstOrDefault();
                if (plan == null) return NotFound(new { error = "Subscription plan not found for planCode", planCode = req.PlanCode });

                var sub = new Onion.Domain.Billing.CompanySubscription
                {
                    CompanyId = user.CompanyId.Value,
                    SubscriptionPlanId = plan.Id,
                    StartDate = req.StartsAt ?? DateTime.UtcNow,
                    RenewalDate = req.NextPaymentAt,
                    Status = Onion.Domain.Billing.SubscriptionStatus.Active
                };

                await _uow.CompanySubscriptions.AddAsync(sub);
                await _uow.SaveChangesAsync();

                return Ok(new { result = "ok", subscriptionId = sub.Id, requestId = Guid.NewGuid().ToString() });
            }
            finally
            {
                Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = null;
            }
        }

        public record CompanySubscriptionByCodeRequest(string PlanCode, DateTime? StartsAt = null, DateTime? NextPaymentAt = null);

        public record UpdateUserRequest(
            string Email,
            string FirstName,
            string LastName,
            string? UserName,
            string? Password
        );

        // 8. Update user details
        [HttpPut("users/{userIdStr}")]
        public async Task<IActionResult> UpdateUser(string userIdStr, [FromBody] UpdateUserRequest req)
        {
            if (req == null) return BadRequest("Body required");

            var user = await FindUserByIdOrExternalIdAsync(userIdStr);
            if (user == null) return NotFound();

            var emailNormalized = req.Email.ToLower().Trim();
            
            Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = user.CompanyId;
            try
            {
                // Check email uniqueness if email is changed
                if (user.Email != emailNormalized)
                {
                    var usersByEmail = await _uow.Users.FindAsync(u => u.Email == emailNormalized);
                    if (usersByEmail.Any())
                    {
                        return Conflict(new { error = "Email already in use" });
                    }
                    user.Email = emailNormalized;
                }

                user.FirstName = req.FirstName;
                user.LastName = string.IsNullOrWhiteSpace(req.LastName) ? "." : req.LastName;
                if (!string.IsNullOrWhiteSpace(req.UserName))
                {
                    user.UserName = req.UserName;
                }
                else
                {
                    user.UserName = req.Email.Split('@')[0];
                }

                if (!string.IsNullOrWhiteSpace(req.Password))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password); // Hash with BCrypt
                }

                _uow.Users.Update(user);
                await _uow.SaveChangesAsync();

                return Ok(new { result = "ok", saasUserId = user.Id, requestId = Guid.NewGuid().ToString() });
            }
            finally
            {
                Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = null;
            }
        }
    }
}
