using System.Security.Claims;
using Onion.DataAccess.Clerk;

namespace crud_onion.Authentication;

public sealed class ClerkOrganizationTenantProvider(IHttpContextAccessor httpContextAccessor) : IOrganizationTenantProvider
{
    public string? GetOrganizationId() => httpContextAccessor.HttpContext?.User.FindFirstValue("org_id");
}
