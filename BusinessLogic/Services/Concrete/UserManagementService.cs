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

namespace Onion.BussinesLogic.Services.Concrete
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using System.Security.Claims;

    public class UserManagementService : IUserManagementService
    {
        private readonly IRepository<User> _userRepo;
        private readonly Onion.DataAccess.OnionDbContext _db;
        private readonly Onion.Common.Services.ICurrentUserService _currentUserService;
        private readonly Onion.Common.Services.IEmailService? _emailService;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string[] _superUserEmails;
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
            var raw = _config["SuperUsers:Emails"] ?? "admin@cuadre.com";
            _superUserEmails = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
        }

        public async Task<PagedResult<UserListDto>> GetPagedAsync(int pageNumber, int pageSize, string? q, string? role, bool? active, int? companyId)
        {
            if (!_currentUserService.IsGlobalAdministrator)
                companyId = _currentUserService.CompanyId;

            var qset = _db.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) qset = qset.Where(u => (u.FirstName + " " + u.LastName).Contains(q) || u.Email.Contains(q));
            if (!string.IsNullOrWhiteSpace(role)) qset = qset.Where(u => u.Role == role);
            if (active.HasValue) qset = qset.Where(u => u.Active == active.Value);
            if (companyId.HasValue) qset = qset.Where(u => u.CompanyId == companyId.Value);
            var dtoQuery = qset.Select(u => new UserListDto(
                u.Id,
                u.FirstName + " " + u.LastName,
                u.FirstName,
                u.LastName,
                u.Email,
                u.UserName,
                u.Role,
                u.CompanyId,
                (string?)null,
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
                 u.LastLoginIp
            ));

            var paged = await DataAccess.Extensions.PaginationExtensions.ToPagedListAsync(dtoQuery, pageNumber, pageSize);

            var result = new PagedResult<UserListDto>
            {
                Items = paged.Items.ToList(),
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

            // Determine company: use provided or current user's company
            var companyId = _currentUserService.IsGlobalAdministrator
                ? (req.CompanyId.HasValue && req.CompanyId.Value <= 0 ? null : req.CompanyId)
                : _currentUserService.CompanyId;

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
                Active = true,
                CreateBy = performedByUserId
                , Address = req.Address
                , City = req.City
                , Country = string.IsNullOrWhiteSpace(req.Country) ? "República Dominicana" : req.Country.Trim()
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
            var allowedRoles = new[] { "Admin", "Supervisor", "Vendedor", "Cajero", "Employee" };
            if (!allowedRoles.Contains(req.Role, StringComparer.OrdinalIgnoreCase) ||
                (!_currentUserService.IsGlobalAdministrator && string.Equals(req.Role, "Admin", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Role is not allowed.");
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
            if (!_currentUserService.IsGlobalAdministrator && u.CompanyId != _currentUserService.CompanyId)
                throw new InvalidOperationException("User not found");
            u.CompanyId = _currentUserService.IsGlobalAdministrator ? req.CompanyId : u.CompanyId;
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
            // Allow configured super-users to perform actions on themselves
            var currentUserId = _currentUserService.UserId;
            var currentEmail = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ?? _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;
            var isSuperUser = !string.IsNullOrEmpty(currentEmail) && _superUserEmails.Any(e => string.Equals(e, currentEmail, StringComparison.OrdinalIgnoreCase));
            if (!isSuperUser && currentUserId.HasValue && currentUserId.Value == id)
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
            // Allow configured super-users to delete themselves
            var currentUserId = _currentUserService.UserId;
            var currentEmail = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ?? _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;
            var isSuperUser = !string.IsNullOrEmpty(currentEmail) && _superUserEmails.Any(e => string.Equals(e, currentEmail, StringComparison.OrdinalIgnoreCase));
            if (!isSuperUser && currentUserId.HasValue && currentUserId.Value == id)
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
            var found = await _userRepo.FindAsync(u => u.Email == email);
            if (excludeId.HasValue) return found.Any(f => f.Id != excludeId.Value);
            return found.Any();
        }

        public async Task<bool> ExistsUserNameAsync(string userName, int? excludeId = null)
        {
            var found = await _userRepo.FindAsync(u => u.UserName == userName);
            if (excludeId.HasValue) return found.Any(f => f.Id != excludeId.Value);
            return found.Any();
        }

        private string GenerateTemporaryPassword()
        {
            return "Temp#" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }
    }
}
