using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace crud_onion.Authentication;

public sealed class ClerkRoleRequirement(string minimumRole) : IAuthorizationRequirement
{
    public string MinimumRole { get; } = minimumRole;
}

public sealed class ClerkRoleHandler : AuthorizationHandler<ClerkRoleRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ClerkRoleRequirement requirement)
    {
        var role = context.User.FindFirstValue("org_role");
        if (RoleAtLeast(role, requirement.MinimumRole))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }

    private static bool RoleAtLeast(string? role, string minimumRole)
    {
        if (string.Equals(role, minimumRole, StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(minimumRole, "org:member", StringComparison.OrdinalIgnoreCase))
            return string.Equals(role, "org:admin", StringComparison.OrdinalIgnoreCase);
        return false;
    }
}
