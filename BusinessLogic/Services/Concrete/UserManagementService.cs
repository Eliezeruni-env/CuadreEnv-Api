using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Dtos;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain.Users;
using Onion.Common.Models.Pagination;
using Onion.Common.Authorization;
using System.Text.Json;

namespace Onion.BussinesLogic.Services.Concrete
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;

    public class UserManagementService : IUserManagementService
    {
        private readonly IRepository<User> _userRepo;
        private readonly Onion.DataAccess.OnionDbContext _db;
        private readonly Onion.Common.Services.ICurrentUserService _currentUserService;
        private readonly Onion.Common.Services.IEmailService? _emailService;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(IRepository<User> userRepo, Onion.DataAccess.OnionDbContext db, Onion.Common.Services.ICurrentUserService currentUserService, ILogger<UserManagementService> logger, Onion.Common.Services.IEmailService? emailService = null, IConfiguration? config = null, IHttpContextAccessor? httpContextAccessor = null)
        {
            _userRepo = userRepo;
            _db = db;
            _currentUserService = currentUserService;
            _emailService = emailService;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger;
        }

        public async Task<PagedResult<UserListDto>> GetPagedAsync(int pageNumber, int pageSize, string? q, string? role, bool? active, int? companyId)
        {
            if (!_currentUserService.IsGlobalAdministrator)
            {
                companyId = _currentUserService.CompanyId;
                if (companyId is not > 0)
                    throw new InvalidOperationException("Tenant company id is required.");
            }

            var qset = _db.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) qset = qset.Where(u => (u.FirstName + " " + u.LastName).Contains(q) || u.Email.Contains(q));
            if (!string.IsNullOrWhiteSpace(role)) qset = qset.Where(u => u.Role == role);
            if (active.HasValue) qset = qset.Where(u => u.Active == active.Value);
            if (companyId.HasValue) qset = qset.Where(u => u.CompanyId == companyId.Value);
            var dtoQuery = qset.Select(u => new
            {
                u.Id,
                FullName = u.FirstName + " " + u.LastName,
                u.FirstName,
                u.LastName,
                u.Email,
                u.UserName,
                u.Role,
                u.CompanyId,
                CompanyName = (string?)null,
                u.Active,
                u.LastLoginAt,
                u.IsSuperUser,
                u.Address,
                u.City,
                u.Country,
                u.OperatingLocation,
                u.IpAddress,
                u.Latitude,
                u.Longitude,
                u.LastLoginIp,
                u.AllowedModulesJson
            });

            var paged = await DataAccess.Extensions.PaginationExtensions.ToPagedListAsync(dtoQuery, pageNumber, pageSize);

            var result = new PagedResult<UserListDto>
            {
                Items = paged.Items.Select(u => new UserListDto(
                    u.Id,
                    u.FullName,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.UserName,
                    u.Role,
                    u.CompanyId,
                    u.CompanyName,
                    u.Active,
                    u.LastLoginAt,
                    u.IsSuperUser,
                    u.Address,
                    u.City,
                    u.Country,
                    u.OperatingLocation,
                    u.IpAddress,
                    u.Latitude,
                    u.Longitude,
                    u.LastLoginIp,
                    ParseAllowedModules(u.AllowedModulesJson, u.Id, u.Role, u.IsSuperUser)
                )).ToList(),
                Total = paged.TotalItemCount,
                Page = pageNumber,
                PageSize = pageSize,
                TotalPages = paged.PageCount
            };

            return result;
        }

        public async Task<UserDetailDto?> GetByIdAsync(int id)
        {
            var u = await _userRepo.GetByIdAsync(id);
            if (u == null) return null;
            if (!_currentUserService.IsGlobalAdministrator && _currentUserService.CompanyId.HasValue)
            {
                var cid = _currentUserService.CompanyId.Value;
                var belongs = u.CompanyId == cid || await _db.UserCompanies.AnyAsync(uc => uc.UserId == u.Id && uc.CompanyId == cid);
                if (!belongs && u.CompanyId.HasValue && u.CompanyId.Value > 0)
                    return null;
            }
            return new UserDetailDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                UserName = u.UserName,
                PhoneNumber = u.PhoneNumber,
                Identification = u.Identification,
                BirthDate = u.BirthDate,
                Gender = u.Gender,
                Role = u.Role,
                CompanyId = u.CompanyId,
                CreationDate = u.CreationDate,
                CreateBy = u.CreateBy,
                ModificationDate = u.ModificationDate,
                ModifiedBy = u.ModifiedBy,
                LastLoginAt = u.LastLoginAt
                ,
                IsSuperUser = u.IsSuperUser,
                AllowedModules = ParseAllowedModules(u.AllowedModulesJson, u.Id, u.Role, u.IsSuperUser),
                Address = u.Address,
                City = u.City,
                Country = u.Country,
                OperatingLocation = u.OperatingLocation,
                IpAddress = u.IpAddress,
                Latitude = u.Latitude,
                Longitude = u.Longitude,
                LastLoginIp = u.LastLoginIp
            };
        }

        public async Task<UserDetailDto> CreateAsync(CreateUserRequest req, string performedByUserId, CancellationToken cancellationToken = default)
        {
            // Required validations
            if (string.IsNullOrWhiteSpace(req.FirstName)) throw new InvalidOperationException("FirstName required");
            if (string.IsNullOrWhiteSpace(req.LastName)) throw new InvalidOperationException("LastName required");
            if (string.IsNullOrWhiteSpace(req.Email)) throw new InvalidOperationException("Email required");
            // Password optional: if not provided, server will generate a temporary password and optionally email it

            var normalizedEmail = req.Email.Trim().ToLowerInvariant();
            var userName = string.IsNullOrWhiteSpace(req.UserName) ? normalizedEmail : req.UserName.Trim();
            if ((await _userRepo.FindAsync(u => u.Email == normalizedEmail)).Any()) throw new InvalidOperationException("Email already in use");
            if ((await _userRepo.FindAsync(u => u.UserName == userName)).Any()) throw new InvalidOperationException("UserName already in use");

            // Tenant users always inherit the authenticated tenant. The request cannot
            // move a user to another company; only global administrators may select it.
            var companyId = _currentUserService.IsGlobalAdministrator
                ? (req.CompanyId.HasValue && req.CompanyId.Value > 0 ? req.CompanyId : null)
                : _currentUserService.CompanyId;

            if (!companyId.HasValue && !_currentUserService.IsGlobalAdministrator)
                throw new InvalidOperationException("Tenant company id is required.");

            if (companyId.HasValue)
            {
                var company = await _db.Companies
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .SingleOrDefaultAsync(c => c.Id == companyId.Value && !c.IsDeleted, cancellationToken);

                if (company is null)
                    throw new InvalidOperationException("The selected company does not exist.");

            }

            var requestedRole = string.IsNullOrWhiteSpace(req.Role) ? "Employee" : req.Role.Trim();
            var allowedRoles = new[] { "Admin", "Supervisor", "Vendedor", "Cajero", "Employee" };
            if (!allowedRoles.Contains(requestedRole, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Role is not allowed.");
            if (!_currentUserService.IsGlobalAdministrator &&
                string.Equals(requestedRole, "Admin", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Tenant administrators cannot assign the Admin role.");

            // Determine password: use provided or generate temporary
            var tempPassword = !string.IsNullOrWhiteSpace(req.TemporaryPassword)
                ? req.TemporaryPassword
                : (!string.IsNullOrWhiteSpace(req.Password) ? req.Password : GenerateTemporaryPassword());
            var passwordToHash = tempPassword;
            var hash = BCrypt.Net.BCrypt.HashPassword(passwordToHash);

            var user = new User
            {
                FirstName = req.FirstName.Trim(),
                LastName = req.LastName.Trim(),
                Email = normalizedEmail,
                UserName = userName,
                PhoneNumber = req.PhoneNumber?.Trim() ?? string.Empty,
                Identification = req.Identification?.Trim(),
                BirthDate = req.BirthDate ?? DateTime.MinValue,
                Gender = string.IsNullOrWhiteSpace(req.Gender) ? "M" : req.Gender.Trim(),
                Role = companyId.HasValue ? requestedRole : "Admin",
                CompanyId = companyId,
                PasswordHash = hash,
                AllowedModulesJson = SerializeAllowedModules(req.AllowedModules),
                Active = req.Active,
                CreateBy = performedByUserId
                , Address = req.Address
                , City = req.City
                , Country = string.IsNullOrWhiteSpace(req.Country) ? "Rep�blica Dominicana" : req.Country.Trim()
                , OperatingLocation = req.OperatingLocation
                , IpAddress = req.IpAddress
                , Latitude = req.Latitude
                , Longitude = req.Longitude
            };

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            await _userRepo.AddAsync(user);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // If password was generated server-side and email service available, send temporary password
            var dto = new UserDetailDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                Identification = user.Identification,
                BirthDate = user.BirthDate,
                Gender = user.Gender,
                Role = user.Role,
                CompanyId = user.CompanyId,
                CreationDate = user.CreationDate,
                CreateBy = user.CreateBy,
                LastLoginAt = user.LastLoginAt,
                TemporaryPassword = null,
                TempPasswordSent = false
                ,
                IsSuperUser = user.IsSuperUser
                , AllowedModules = ParseAllowedModules(user.AllowedModulesJson, user.Id, user.Role, user.IsSuperUser)
                , Address = user.Address
                , City = user.City
                , Country = user.Country
                , OperatingLocation = user.OperatingLocation
                , IpAddress = user.IpAddress
                , Latitude = user.Latitude
                , Longitude = user.Longitude
                , LastLoginIp = user.LastLoginIp
            };

            if (req.SendByEmail && _emailService != null)
            {
                try
                {
                    var subject = "[CuadreEnv] Your account has been created";
                    var body = $"Hello {user.FirstName},\n\nYour account has been created. Temporary password: {tempPassword}\nPlease login and change your password.";
                    await _emailService.SendEmailAsync(user.Email, subject, body);
                    dto.TempPasswordSent = true;
                }
                catch
                {
                    dto.TemporaryPassword = tempPassword;
                    dto.TempPasswordSent = false;
                }
            }
            else
            {
                if (!req.SendByEmail || _emailService == null)
                    dto.TemporaryPassword = tempPassword;
                dto.TempPasswordSent = false;
            }

            return dto;
        }

        public async Task UpdateAsync(int id, UpdateUserRequest req, string performedByUserId)
        {
            var u = await _userRepo.GetByIdAsync(id);
            if (u == null) throw new InvalidOperationException("User not found");
            var isCurrentAdmin = _currentUserService.IsGlobalAdministrator ||
                string.Equals(_currentUserService.UserRole, "Admin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_currentUserService.UserRole, "Administrador", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_currentUserService.UserRole, "Owner", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_currentUserService.UserRole, "Dueño", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_currentUserService.UserRole, "Dueno", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_currentUserService.UserRole, "Gerente", StringComparison.OrdinalIgnoreCase);

            var allowedRoles = new[] { "Admin", "Administrador", "Supervisor", "Vendedor", "Cajero", "Employee", "Empleado" };
            if (!allowedRoles.Contains(req.Role, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("El rol seleccionado no es válido.");

            if (!isCurrentAdmin && (string.Equals(req.Role, "Admin", StringComparison.OrdinalIgnoreCase) || string.Equals(req.Role, "Administrador", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("No tienes permisos para asignar el rol de Administrador.");

            if (u.Email != req.Email && (await _userRepo.FindAsync(x => x.Email == req.Email)).Any()) throw new InvalidOperationException("Email already in use");
            if (u.UserName != req.UserName && (await _userRepo.FindAsync(x => x.UserName == req.UserName)).Any()) throw new InvalidOperationException("UserName already in use");

            u.FirstName = req.FirstName;
            u.LastName = req.LastName;
            u.Email = req.Email;
            u.UserName = req.UserName;
            u.PhoneNumber = req.PhoneNumber ?? string.Empty;
            u.Identification = req.Identification;
            if (req.BirthDate.HasValue)
                u.BirthDate = req.BirthDate.Value;
            u.Gender = req.Gender;
            u.Role = req.Role;
            if (req.AllowedModules is not null)
                u.AllowedModulesJson = SerializeAllowedModules(req.AllowedModules);
            if (!_currentUserService.IsGlobalAdministrator && _currentUserService.CompanyId.HasValue)
            {
                var cid = _currentUserService.CompanyId.Value;
                var belongs = u.CompanyId == cid || await _db.UserCompanies.AnyAsync(uc => uc.UserId == u.Id && uc.CompanyId == cid);
                if (!belongs && u.CompanyId.HasValue && u.CompanyId.Value > 0)
                    throw new InvalidOperationException("User not found");
            }
            if (_currentUserService.IsGlobalAdministrator && req.CompanyId.HasValue && req.CompanyId != u.CompanyId)
            {
                var companyExists = await _db.Companies.IgnoreQueryFilters()
                    .AnyAsync(c => c.Id == req.CompanyId.Value && !c.IsDeleted);
                if (!companyExists)
                    throw new InvalidOperationException("The selected company does not exist.");
                u.CompanyId = req.CompanyId;
            }
            u.ModificationDate = DateTime.UtcNow;
            u.ModifiedBy = performedByUserId;
            u.Address = req.Address;
            u.City = req.City;
            if (!string.IsNullOrWhiteSpace(req.Country)) u.Country = req.Country.Trim();
            u.OperatingLocation = req.OperatingLocation;
            u.IpAddress = req.IpAddress;
            u.Latitude = req.Latitude;
            u.Longitude = req.Longitude;

            _userRepo.Update(u);
            await _db.SaveChangesAsync();
        }

        public async Task ToggleActiveAsync(int id, bool active, string performedByUserId)
        {
            var u = await _userRepo.GetByIdAsync(id);
            if (u == null) throw new InvalidOperationException("User not found");
            if (!_currentUserService.IsGlobalAdministrator && u.CompanyId != _currentUserService.CompanyId)
                throw new InvalidOperationException("User not found");
            var currentUserId = _currentUserService.UserId;
            if (!_currentUserService.IsGlobalAdministrator && currentUserId.HasValue && currentUserId.Value == id)
                throw new InvalidOperationException("Cannot change your own active status");
            u.Active = active;
            u.ModificationDate = DateTime.UtcNow;
            u.ModifiedBy = performedByUserId;
            _userRepo.Update(u);
            await _db.SaveChangesAsync();
        }

        // Interface expects ResetPasswordAsync with string performedByUserId and returning string result; implement both overloads for compatibility
        public async Task<string> ResetPasswordAsync(int id, ResetPasswordRequest req, string performedByUserId)
        {
            var u = await _userRepo.GetByIdAsync(id);
            if (u == null) throw new InvalidOperationException("User not found");
            if (!_currentUserService.IsGlobalAdministrator && u.CompanyId != _currentUserService.CompanyId)
                throw new InvalidOperationException("User not found");

            var temp = req.TemporaryPassword ?? GenerateTemporaryPassword();
            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temp);
            u.ModificationDate = DateTime.UtcNow;
            u.ModifiedBy = performedByUserId;
            _userRepo.Update(u);
            await _db.SaveChangesAsync();

            if (req.SendByEmail && _emailService != null)
            {
                var subject = "[CuadreEnv] Password reset";
                var body = $"Your temporary password is: {temp}. Use it to login and change your password.";
                try
                {
                    await _emailService.SendEmailAsync(u.Email, subject, body);
                    return "sent";
                }
                catch { }
            }

            return temp;
        }

        // Backwards-compatible signature used earlier in code: accepts int performedByUserId
        public async Task ResetPasswordAsync(int id, ResetPasswordRequest req, int performedByUserId)
        {
            await ResetPasswordAsync(id, req, performedByUserId.ToString());
        }

        public async Task DeleteAsync(int id, string performedByUserId)
        {
            var u = await _userRepo.GetByIdAsync(id);
            if (u == null) throw new InvalidOperationException("User not found");
            if (!_currentUserService.IsGlobalAdministrator && u.CompanyId != _currentUserService.CompanyId)
                throw new InvalidOperationException("User not found");
            var currentUserId = _currentUserService.UserId;
            if (!_currentUserService.IsGlobalAdministrator && currentUserId.HasValue && currentUserId.Value == id)
                throw new InvalidOperationException("Cannot delete your own account");
            u.IsDeleted = true;
            u.Active = false;
            u.ModificationDate = DateTime.UtcNow;
            u.ModifiedBy = performedByUserId;
            _userRepo.Update(u);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> ExistsEmailAsync(string email, int? excludeId = null)
        {
            var normalized = email.Trim().ToLowerInvariant();
            var found = await _userRepo.FindAsync(u => u.Email.ToLower() == normalized &&
                (!excludeId.HasValue || u.Id != excludeId.Value));
            return found.Any();
        }

        public async Task<bool> ExistsUserNameAsync(string userName, int? excludeId = null)
        {
            var normalized = userName.Trim().ToLowerInvariant();
            var found = await _userRepo.FindAsync(u => u.UserName.ToLower() == normalized &&
                (!excludeId.HasValue || u.Id != excludeId.Value));
            return found.Any();
        }

        private string GenerateTemporaryPassword()
        {
            return "Temp#" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        private static string SerializeAllowedModules(IEnumerable<string>? moduleCodes)
        {
            var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var moduleCode in moduleCodes ?? Array.Empty<string>())
            {
                var literal = moduleCode?.Trim();
                if (string.IsNullOrWhiteSpace(literal)) continue;
                if (string.Equals(literal, ModuleCodes.All, StringComparison.Ordinal) ||
                    string.Equals(literal, "all", StringComparison.OrdinalIgnoreCase))
                {
                    assigned.Add(ModuleCodes.All);
                    continue;
                }

                if (ModuleCodes.Normalize(literal) is not null)
                {
                    assigned.Add(literal.ToLowerInvariant());
                }
                else
                {
                    assigned.Add(literal.ToLowerInvariant());
                }
            }

            return JsonSerializer.Serialize(assigned.ToList());
        }

        private List<string> ParseAllowedModules(string? allowedModulesJson, int userId, string? role = null, bool isSuperUser = false)
        {
            if (string.Equals(allowedModulesJson?.Trim(), "[]", StringComparison.Ordinal))
            {
                return new List<string>();
            }

            if (!string.IsNullOrWhiteSpace(allowedModulesJson))
            {
                try
                {
                    var parsed = ModuleCodes.FromClaimPreservingIds(allowedModulesJson).ToList();
                    if (parsed.Count > 0) return parsed;
                }
                catch (JsonException ex)
                {
                    _logger?.LogWarning(ex, "Invalid AllowedModulesJson for user {UserId}", userId);
                }
            }

            if (isSuperUser ||
                string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "Administrador", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "Dueño", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "Dueno", StringComparison.OrdinalIgnoreCase))
            {
                return new List<string> { ModuleCodes.All };
            }

            if (string.Equals(role, "Supervisor", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "dashboard", "sales", "cashregister", "inventory", "customers", "audit", "reports", "users" };
            if (string.Equals(role, "Cajero", StringComparison.OrdinalIgnoreCase) || string.Equals(role, "Cashier", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "dashboard", "sales", "cashregister", "customers", "products.detail_sales" };
            if (string.Equals(role, "Vendedor", StringComparison.OrdinalIgnoreCase) || string.Equals(role, "Seller", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "dashboard", "sales", "inventory", "customers" };

            return new List<string> { "dashboard", "sales", "cashregister", "customers" };
        }
    }
}
