using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Onion.Common.Services;
using System.Security.Claims;
using Onion.Common.Authorization;

namespace Onion.Common.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class AuthorizeModuleAttribute : TypeFilterAttribute
{
    public AuthorizeModuleAttribute(string moduleCode)
        : base(typeof(AuthorizeModuleFilter)) => Arguments = new object[] { moduleCode };
}

 [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AuthorizeAnyModuleAttribute : TypeFilterAttribute
{
    public AuthorizeAnyModuleAttribute(params string[] moduleCodes)
        : base(typeof(AuthorizeAnyModuleFilter)) => Arguments = new object[] { moduleCodes };
}

public sealed class AuthorizeModuleFilter : IAsyncActionFilter
{
    private readonly ICompanyEntitlementService _entitlements;
    private readonly string _moduleCode;
    private readonly ILogger<AuthorizeModuleFilter>? _logger;

    public AuthorizeModuleFilter(ICompanyEntitlementService entitlements, string moduleCode, ILogger<AuthorizeModuleFilter>? logger = null)
    {
        _entitlements = entitlements;
        _moduleCode = moduleCode;
        _logger = logger;
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
            context.Result = Forbidden(user, 0, "COMPANY_REQUIRED", "El usuario no tiene una empresa válida asignada.", _moduleCode, Array.Empty<string>());
            return;
        }

        var requestedModule = ModuleCodes.Normalize(_moduleCode);
        var allowedModules = user.FindClaims("module", "AllowedModulesJson", "allowedModulesJson", "allowedModules", "allowed_modules", "modules")
            .SelectMany(ModuleCodes.FromClaim)
            .ToHashSet(StringComparer.Ordinal);

        if (allowedModules.Count == 0)
        {
            context.Result = Forbidden(user, companyId, "MODULE_NOT_ASSIGNED", "Su cuenta no tiene módulos asignados. Contacte al administrador.", requestedModule ?? _moduleCode, allowedModules);
            return;
        }

        if (requestedModule is null || (!allowedModules.Contains(ModuleCodes.All) && !allowedModules.Contains(requestedModule)))
        {
            context.Result = Forbidden(user, companyId, "MODULE_NOT_ASSIGNED", "No tienes acceso al módulo requerido. Contacta al administrador para ampliar tu licencia.", requestedModule ?? _moduleCode, allowedModules);
            return;
        }

        var access = await _entitlements.GetAsync(companyId, context.HttpContext.RequestAborted);
        var licensedModules = ModuleCodes.Normalize(access.Modules);
        if (!licensedModules.Contains(ModuleCodes.All) && !licensedModules.Contains(requestedModule))
        {
            context.Result = Forbidden(user, companyId, "MODULE_NOT_LICENSED", "No tienes acceso al módulo requerido. Contacta al administrador para ampliar tu licencia.", requestedModule, allowedModules);
            return;
        }

        await next();
    }

    private ObjectResult Forbidden(ClaimsPrincipal user, int companyId, string code, string message, string requiredModule, IEnumerable<string> assignedModules)
    {
        var assigned = assignedModules.Distinct(StringComparer.Ordinal).ToArray();
        _logger?.LogWarning("Module access denied. UserId={UserId}, CompanyId={CompanyId}, RequiredModule={RequiredModule}, AssignedModules={AssignedModules}, Code={Code}",
            user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown",
            companyId, requiredModule, string.Join(',', assigned), code);
        return new ObjectResult(new
        {
            success = false,
            errorCode = code,
            message,
            requiredModule,
            assignedModules = assigned,
            error = message,
            code
        }) { StatusCode = StatusCodes.Status403Forbidden };
    }

    private static bool IsGlobalAdministrator(ClaimsPrincipal user) =>
        user.Claims.Any(c => string.Equals(c.Type, "isSuperUser", StringComparison.OrdinalIgnoreCase) && string.Equals(c.Value, "true", StringComparison.OrdinalIgnoreCase)) ||
        user.Claims.Any(c => (string.Equals(c.Type, "role", StringComparison.OrdinalIgnoreCase) || c.Type == ClaimTypes.Role) &&
                             string.Equals(c.Value, "SuperAdmin", StringComparison.OrdinalIgnoreCase));
}

public sealed class AuthorizeAnyModuleFilter : IAsyncActionFilter
{
    private readonly ICompanyEntitlementService _entitlements;
    private readonly string[] _moduleCodes;
    private readonly ILogger<AuthorizeAnyModuleFilter>? _logger;

    public AuthorizeAnyModuleFilter(ICompanyEntitlementService entitlements, string[] moduleCodes, ILogger<AuthorizeAnyModuleFilter>? logger = null)
    {
        _entitlements = entitlements;
        _moduleCodes = moduleCodes;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var companyClaim = user.FindFirst("companyId") ?? user.FindFirst("CompanyId");
        if (!int.TryParse(companyClaim?.Value, out var companyId) || companyId <= 0)
        {
            _logger?.LogWarning("Module access denied. UserId={UserId}, RequiredModules={RequiredModules}, Code=COMPANY_REQUIRED", UserId(user), string.Join(',', _moduleCodes));
            context.Result = new ObjectResult(new
            {
                success = false,
                errorCode = "COMPANY_REQUIRED",
                code = "COMPANY_REQUIRED",
                message = "El usuario no tiene una empresa válida asignada."
            }) { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        var requestedModules = ModuleCodes.Normalize(_moduleCodes).ToHashSet(StringComparer.Ordinal);
        if (requestedModules.Count == 0)
        {
            _logger?.LogWarning("Module access denied. UserId={UserId}, CompanyId={CompanyId}, Code=MODULE_NOT_ASSIGNED", UserId(user), companyId);
            context.Result = new ObjectResult(new
            {
                success = false,
                errorCode = "MODULE_NOT_ASSIGNED",
                code = "MODULE_NOT_ASSIGNED",
                message = "No hay módulos habilitados para consultar este recurso.",
                requiredModule = string.Join(',', requestedModules),
                assignedModules = Array.Empty<string>()
            }) { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        var allowedModules = user.FindClaims("module", "AllowedModulesJson", "allowedModulesJson", "allowedModules", "allowed_modules", "modules")
            .SelectMany(ModuleCodes.FromClaim)
            .ToHashSet(StringComparer.Ordinal);
        var hasAssignedModule = allowedModules.Contains(ModuleCodes.All) ||
                                requestedModules.Overlaps(allowedModules);
        if (!hasAssignedModule)
        {
            _logger?.LogWarning("Module access denied. UserId={UserId}, CompanyId={CompanyId}, RequiredModules={RequiredModules}, AssignedModules={AssignedModules}, Code=MODULE_ACCESS_DENIED", UserId(user), companyId, string.Join(',', requestedModules), string.Join(',', allowedModules));
            context.Result = new ObjectResult(new
            {
                success = false,
                errorCode = "MODULE_ACCESS_DENIED",
                code = "MODULE_NOT_ASSIGNED",
                message = allowedModules.Count == 0
                    ? "Su cuenta no tiene módulos asignados. Contacte al administrador."
                    : "El usuario no tiene asignado un módulo que permita consultar este recurso.",
                requiredModule = string.Join(',', requestedModules),
                assignedModules = allowedModules
            }) { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        var entitlements = await _entitlements.GetAsync(companyId, context.HttpContext.RequestAborted);
        var licensedModules = ModuleCodes.Normalize(entitlements.Modules).ToHashSet(StringComparer.Ordinal);
        if (!licensedModules.Contains(ModuleCodes.All) && !requestedModules.Overlaps(licensedModules))
        {
            _logger?.LogWarning("Module access denied. UserId={UserId}, CompanyId={CompanyId}, RequiredModules={RequiredModules}, AssignedModules={AssignedModules}, Code=MODULE_ACCESS_DENIED", UserId(user), companyId, string.Join(',', requestedModules), string.Join(',', allowedModules));
            context.Result = new ObjectResult(new
            {
                success = false,
                errorCode = "MODULE_ACCESS_DENIED",
                code = "MODULE_NOT_LICENSED",
                message = "Su empresa no tiene contratado o activado un módulo compatible en su plan actual.",
                requiredModule = string.Join(',', requestedModules),
                assignedModules = allowedModules
            }) { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        await next();
    }

    private static string UserId(ClaimsPrincipal user) =>
        user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
}

internal static class ClaimsPrincipalModuleExtensions
{
    public static IEnumerable<string> FindClaims(this ClaimsPrincipal user, params string[] types) =>
        user.Claims.Where(c => types.Any(type => string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase)))
            .Select(c => c.Value);
}
