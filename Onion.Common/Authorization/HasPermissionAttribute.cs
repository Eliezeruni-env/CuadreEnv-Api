using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Onion.Common.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : TypeFilterAttribute
{
    public HasPermissionAttribute(string module, string action)
        : base(typeof(HasPermissionFilter))
    {
        Arguments = new object[] { module, action };
    }
}

public sealed class HasPermissionFilter : IAsyncActionFilter
{
    private readonly string _module;
    private readonly string _action;

    public HasPermissionFilter(string module, string action)
    {
        _module = module;
        _action = action;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        var allowed = user.Identity?.IsAuthenticated == true &&
            (IsRole(user, "SuperAdmin") || IsRole(user, "Admin") || HasPermission(user));
        if (!allowed)
        {
            context.Result = user.Identity?.IsAuthenticated == true ? new ForbidResult() : new UnauthorizedResult();
            return;
        }
        await next();
    }

    private bool HasPermission(ClaimsPrincipal user)
    {
        var expected = $"{_module}:{_action}";
        return user.Claims.Any(c =>
            (string.Equals(c.Type, "permission", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(c.Type, "permissions", StringComparison.OrdinalIgnoreCase)) &&
            (string.Equals(c.Value, expected, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(c.Value, $"{_module}.{_action}", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsRole(ClaimsPrincipal user, string role) =>
        user.Claims.Any(c => (c.Type == ClaimTypes.Role || string.Equals(c.Type, "role", StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(c.Value, role, StringComparison.OrdinalIgnoreCase));
}
