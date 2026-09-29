using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Onion.Common.Services;
using System.Security.Claims;

namespace Onion.Common.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class AuthorizeModuleAttribute : TypeFilterAttribute
{
    public AuthorizeModuleAttribute(string moduleCode)
        : base(typeof(AuthorizeModuleFilter)) => Arguments = new object[] { moduleCode };
}

public sealed class AuthorizeModuleFilter : IAsyncActionFilter
{
    private readonly ICompanyEntitlementService _entitlements;
    private readonly string _moduleCode;

    public AuthorizeModuleFilter(ICompanyEntitlementService entitlements, string moduleCode)
    {
        _entitlements = entitlements;
        _moduleCode = moduleCode;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (IsGlobalAdministrator(user))
        {
            await next();
            return;
        }

        var claim = user.FindFirst("companyId") ?? user.FindFirst("CompanyId");
        if (!int.TryParse(claim?.Value, out var companyId) || companyId <= 0)
        {
            context.Result = Forbidden("COMPANY_REQUIRED", "El usuario no tiene una empresa válida asignada.");
            return;
        }

        var access = await _entitlements.GetAsync(companyId, context.HttpContext.RequestAborted);
        if (!access.Modules.Contains(_moduleCode))
        {
            context.Result = Forbidden("MODULE_NOT_LICENSED", "Su empresa no tiene contratado o activado este módulo en su plan actual.");
            return;
        }

        await next();
    }

    private static ObjectResult Forbidden(string code, string message) => new(new
    {
        success = false,
        code,
        message
    }) { StatusCode = StatusCodes.Status403Forbidden };

    private static bool IsGlobalAdministrator(ClaimsPrincipal user) =>
        user.Claims.Any(c => string.Equals(c.Type, "isSuperUser", StringComparison.OrdinalIgnoreCase) && string.Equals(c.Value, "true", StringComparison.OrdinalIgnoreCase)) ||
        user.IsInRole("SuperAdmin") || user.IsInRole("SysAdmin") ||
        user.IsInRole("Admin") ||
        user.Claims.Any(c => (c.Type == "role" || c.Type == ClaimTypes.Role || c.Type.EndsWith("/role")) &&
            (string.Equals(c.Value, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(c.Value, "SysAdmin", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase)));
}
