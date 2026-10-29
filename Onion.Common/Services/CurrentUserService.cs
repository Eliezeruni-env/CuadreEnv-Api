using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Security.Claims;

namespace Onion.Common.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public string? UserEmail => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;

        public string? ClerkUserId => _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public string? UserRole => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value;

        public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        public string? UserAgent => _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

        public int? CompanyId
        {
            get
            {
                try
                {
                    var ctx = _httpContextAccessor.HttpContext;
                    if (ctx?.Items.TryGetValue("TenantCompanyId", out var resolved) == true &&
                        resolved is int resolvedId && resolvedId > 0)
                        return resolvedId;

                    var header = ctx?.Request.Headers["X-Company-Id"].FirstOrDefault()
                        ?? ctx?.Request.Headers["x-company-id"].FirstOrDefault()
                        ?? ctx?.Request.Headers["X-Tenant-Id"].FirstOrDefault()
                        ?? ctx?.Request.Headers["x-tenant-id"].FirstOrDefault();
                    if (int.TryParse(header, out var headerId) && headerId > 0)
                        return headerId;

                    var claim = ctx?.User?.FindFirst("companyId") 
                        ?? ctx?.User?.FindFirst("CompanyId")
                        ?? ctx?.User?.FindFirst("company_id")
                        ?? ctx?.User?.FindFirst("tenant_id")
                        ?? ctx?.User?.FindFirst("tenantId")
                        ?? ctx?.User?.FindFirst("org_id")
                        ?? ctx?.User?.FindFirst("OrganizationId")
                        ?? ctx?.User?.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid")
                        ?? ctx?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid");
                    if (claim != null && int.TryParse(claim.Value, out var id) && id > 0)
                        return id;

                    return null;
                }
                catch
                {
                    return null;
                }
            }
        }

        public int? UserId
        {
            get
            {
                try
                {
                    var ctx = _httpContextAccessor.HttpContext;
                    var claim = ctx?.User?.FindFirst("sub")
                        ?? ctx?.User?.FindFirst(ClaimTypes.NameIdentifier);
                    if (claim == null) return null;
                    if (int.TryParse(claim.Value, out var id)) return id;
                    return null;
                }
                catch
                {
                    return null;
                }
            }
        }

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

        public bool IsGlobalAdministrator
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user?.Identity?.IsAuthenticated != true) return false;

                var isSuperUser = user.Claims.Any(c =>
                    string.Equals(c.Type, "isSuperUser", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(c.Value, "true", StringComparison.OrdinalIgnoreCase));
                if (isSuperUser) return true;

                var role = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value;
                var isSystemAdministrator = string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(role, "SysAdmin", StringComparison.OrdinalIgnoreCase);
                if (isSystemAdministrator) return true;

                return false;
            }
        }
    }
}
