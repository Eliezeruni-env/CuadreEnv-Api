using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Onion.Common.Authorization
{
    public class AuthorizationService : IAuthorizationService
    {
        public bool IsAuthenticated(ClaimsPrincipal user)
        {
            return user?.Identity != null && user.Identity.IsAuthenticated;
        }

        public string? GetRole(ClaimsPrincipal user)
        {
            if (user == null) return null;
            // Prefer ClaimTypes.Role then fallback to lowercase "role"
            var rc = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value;
            return string.IsNullOrWhiteSpace(rc) ? null : rc;
        }

        public bool IsInRole(ClaimsPrincipal user, string role)
        {
            if (!IsAuthenticated(user) || string.IsNullOrWhiteSpace(role)) return false;

            // Use ClaimsPrincipal.IsInRole when available (framework integration), else compare claim values.
            try
            {
                if (user.IsInRole(role)) return true;
            }
            catch
            {
                // Some test principals may not support IsInRole; fall back to claim checks below.
            }

            var rc = GetRole(user);
            return rc != null && string.Equals(rc, role, StringComparison.OrdinalIgnoreCase);
        }

        public bool CheckTenantMatch(ClaimsPrincipal user, int resourceCompanyId)
        {
            if (!IsAuthenticated(user)) return false;
            if (!TryGetCompanyId(user, out var companyId)) return false;
            return companyId == resourceCompanyId;
        }

        public bool TryGetCompanyId(ClaimsPrincipal user, out int companyId)
        {
            companyId = 0;
            if (user == null) return false;
            var claim = user.FindFirst("CompanyId")?.Value;
            if (!int.TryParse(claim, out var parsed)) return false;
            companyId = parsed;
            return true;
        }

        public bool HasPermission(ClaimsPrincipal user, string permission)
        {
            if (!IsAuthenticated(user) || string.IsNullOrWhiteSpace(permission)) return false;

            if (!RolesConstants.PermissionToRoles.TryGetValue(permission, out var allowedRoles))
                return false; // Unknown permission => deny

            var role = GetRole(user);
            if (role == null) return false;

            return allowedRoles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        }

        public Task<bool> AuthorizeAsync(ClaimsPrincipal user, string[]? requiredRoles = null, string[]? requiredPermissions = null, int? resourceCompanyId = null)
        {
            if (!IsAuthenticated(user)) return Task.FromResult(false);

            // Role checks (if provided)
            if (requiredRoles != null && requiredRoles.Length > 0)
            {
                foreach (var r in requiredRoles)
                {
                    if (IsInRole(user, r))
                        return Task.FromResult(true);
                }
                // If role not matched, allow tenant match if resourceCompanyId provided
                if (resourceCompanyId.HasValue && CheckTenantMatch(user, resourceCompanyId.Value))
                    return Task.FromResult(true);
                return Task.FromResult(false);
            }

            // Permission checks (if provided)
            if (requiredPermissions != null && requiredPermissions.Length > 0)
            {
                foreach (var p in requiredPermissions)
                {
                    if (HasPermission(user, p))
                        return Task.FromResult(true);
                }
                // If permission not granted, allow tenant match if resourceCompanyId provided
                if (resourceCompanyId.HasValue && CheckTenantMatch(user, resourceCompanyId.Value))
                    return Task.FromResult(true);
                return Task.FromResult(false);
            }

            // If resourceCompanyId provided, require tenant match as fallback
            if (resourceCompanyId.HasValue)
            {
                return Task.FromResult(CheckTenantMatch(user, resourceCompanyId.Value));
            }

            // No explicit requirements -> allow
            return Task.FromResult(true);
        }
    }
}
