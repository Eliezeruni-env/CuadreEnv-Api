using System.Security.Claims;
using System.Threading.Tasks;

namespace Onion.Common.Authorization
{
    public interface IAuthorizationService
    {
        bool IsAuthenticated(ClaimsPrincipal user);
        string? GetRole(ClaimsPrincipal user);
        bool IsInRole(ClaimsPrincipal user, string role);
        bool CheckTenantMatch(ClaimsPrincipal user, int resourceCompanyId);
        bool TryGetCompanyId(ClaimsPrincipal user, out int companyId);
        bool HasPermission(ClaimsPrincipal user, string permission);
        Task<bool> AuthorizeAsync(ClaimsPrincipal user, string[]? requiredRoles = null, string[]? requiredPermissions = null, int? resourceCompanyId = null);
    }
}
