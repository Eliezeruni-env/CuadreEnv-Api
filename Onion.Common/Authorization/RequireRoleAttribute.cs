using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Onion.Common.Authorization
{
    // Simple roles enum to allow attribute usage like [RequireRole(Roles.Admin, Roles.Manager)]
    public enum Roles { Admin, Manager, Employee }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireRoleAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string[] _allowedRoles;
        private readonly bool _requireTenantMatch;
        private readonly string? _routeCompanyIdParameter;

        public RequireRoleAttribute(bool requireTenantMatch = false, params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles ?? Array.Empty<string>();
            _requireTenantMatch = requireTenantMatch;
        }

        // Preserve previous simple overloads for attribute usage like [RequireRole(Roles.Admin, Roles.Manager)]
        public RequireRoleAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles ?? Array.Empty<string>();
            _requireTenantMatch = false;
        }

        public RequireRoleAttribute(params Roles[] allowedRoles)
        {
            _allowedRoles = allowedRoles?.Select(r => r.ToString())?.ToArray() ?? Array.Empty<string>();
            _requireTenantMatch = false;
        }

        public RequireRoleAttribute(bool requireTenantMatch = false, params Roles[] allowedRoles)
        {
            _allowedRoles = allowedRoles?.Select(r => r.ToString())?.ToArray() ?? Array.Empty<string>();
            _requireTenantMatch = requireTenantMatch;
        }

        // Optional: specify route parameter name containing the resource CompanyId
        public RequireRoleAttribute(string routeCompanyIdParameter, bool requireTenantMatch = true, params Roles[] allowedRoles)
        {
            _allowedRoles = allowedRoles?.Select(r => r.ToString())?.ToArray() ?? Array.Empty<string>();
            _requireTenantMatch = requireTenantMatch;
            _routeCompanyIdParameter = routeCompanyIdParameter;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            var passed = CheckAccess(user, context.RouteData.Values.ToDictionary(k => k.Key, v => v.Value), context.ActionArguments);
            if (!passed)
            {
                // If user is not authenticated -> Unauthorized, otherwise Forbid
                if (user?.Identity == null || !user.Identity.IsAuthenticated)
                    context.Result = new UnauthorizedResult();
                else
                    context.Result = new ForbidResult();
                return;
            }

            await next();
        }

        // Allow unit tests to verify logic without ASP.NET types
        public bool CheckAccess(System.Security.Claims.ClaimsPrincipal? user, IDictionary<string, object?>? routeValues = null, IDictionary<string, object>? actionArguments = null)
        {
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
                return false;

            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value;
            if (string.IsNullOrWhiteSpace(roleClaim) || !_allowedRoles.Contains(roleClaim, StringComparer.OrdinalIgnoreCase))
                return false;

            if (!_requireTenantMatch)
                return true;

            var companyClaim = user.FindFirst("CompanyId")?.Value;
            if (!int.TryParse(companyClaim, out var companyId))
                return false;

            int? resourceCompanyId = null;

            if (!string.IsNullOrWhiteSpace(_routeCompanyIdParameter) && routeValues != null && routeValues.TryGetValue(_routeCompanyIdParameter, out var rv) && rv != null)
            {
                if (int.TryParse(rv.ToString(), out var parsed)) resourceCompanyId = parsed;
            }

            if (!resourceCompanyId.HasValue && routeValues != null)
            {
                if (routeValues.TryGetValue("companyId", out var v2) && v2 != null && int.TryParse(v2.ToString(), out var p2)) resourceCompanyId = p2;
                else if (routeValues.TryGetValue("id", out var v3) && v3 != null && int.TryParse(v3.ToString(), out var p3)) resourceCompanyId = p3;
            }

            if (!resourceCompanyId.HasValue && actionArguments != null)
            {
                foreach (var arg in actionArguments.Values)
                {
                    if (arg == null) continue;
                    var prop = arg.GetType().GetProperty("CompanyId");
                    if (prop != null)
                    {
                        var argVal = prop.GetValue(arg);
                        if (argVal != null && int.TryParse(argVal.ToString(), out var parsed))
                        {
                            resourceCompanyId = parsed;
                            break;
                        }
                    }
                }
            }

            if (resourceCompanyId.HasValue && resourceCompanyId.Value != companyId)
                return false;

            return true;
        }
    }
}
