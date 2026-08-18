using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Onion.Common.Authorization
{
    // Use TypeFilterAttribute so the inner filter can receive DI services.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequirePermissionAttribute : TypeFilterAttribute
    {
        public RequirePermissionAttribute(string[]? requiredRoles = null, string[]? requiredPermissions = null, string? routeCompanyIdParameter = null)
            : base(typeof(RequirePermissionFilter))
        {
            Arguments = new object[] { requiredRoles ?? Array.Empty<string>(), requiredPermissions ?? Array.Empty<string>(), routeCompanyIdParameter };
        }
    }

    public class RequirePermissionFilter : IAsyncActionFilter
    {
        private readonly IAuthorizationService _auth;
        private readonly string[] _requiredRoles;
        private readonly string[] _requiredPermissions;
        private readonly string? _routeCompanyIdParameter;

        public RequirePermissionFilter(IAuthorizationService auth, string[] requiredRoles, string[] requiredPermissions, string? routeCompanyIdParameter)
        {
            _auth = auth;
            _requiredRoles = requiredRoles ?? Array.Empty<string>();
            _requiredPermissions = requiredPermissions ?? Array.Empty<string>();
            _routeCompanyIdParameter = routeCompanyIdParameter;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;

            int? resourceCompanyId = null;
            // check route param first
            if (!string.IsNullOrWhiteSpace(_routeCompanyIdParameter) && context.RouteData.Values.TryGetValue(_routeCompanyIdParameter, out var rv) && rv != null)
            {
                if (int.TryParse(rv.ToString(), out var parsed)) resourceCompanyId = parsed;
            }

            // fallback: common route names
            if (!resourceCompanyId.HasValue)
            {
                // Only attempt fallback discovery of common route names when no explicit role/permission requirements are set.
                // If required roles/permissions are provided, caller should specify the route param name to avoid accidental tenant-based authorization bypass.
                if (_requiredRoles.Length == 0 && _requiredPermissions.Length == 0)
                {
                    if (context.RouteData.Values.TryGetValue("companyId", out var v2) && v2 != null && int.TryParse(v2.ToString(), out var p2)) resourceCompanyId = p2;
                    else if (context.RouteData.Values.TryGetValue("id", out var v3) && v3 != null && int.TryParse(v3.ToString(), out var p3)) resourceCompanyId = p3;
                }
            }

            // fallback: inspect action arguments for CompanyId
            if (!resourceCompanyId.HasValue)
            {
                foreach (var arg in context.ActionArguments.Values)
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

            var allowed = await _auth.AuthorizeAsync(user, _requiredRoles.Length > 0 ? _requiredRoles : null, _requiredPermissions.Length > 0 ? _requiredPermissions : null, resourceCompanyId);

            if (!allowed)
            {
                if (user?.Identity == null || !user.Identity.IsAuthenticated)
                    context.Result = new UnauthorizedResult();
                else
                    context.Result = new ForbidResult();
                return;
            }

            await next();
        }

        // Publicly testable helper mirroring logic above
        public bool CheckAccess(ClaimsPrincipal? user, IDictionary<string, object?>? routeValues = null, IDictionary<string, object>? actionArguments = null)
        {
            if (!_auth.IsAuthenticated(user)) return false;

            int? resourceCompanyId = null;
            if (!string.IsNullOrWhiteSpace(_routeCompanyIdParameter) && routeValues != null && routeValues.TryGetValue(_routeCompanyIdParameter, out var rv) && rv != null)
            {
                if (int.TryParse(rv.ToString(), out var parsed)) resourceCompanyId = parsed;
            }

            if (!resourceCompanyId.HasValue && routeValues != null)
            {
                // Only attempt fallback discovery of common route names when no explicit role/permission requirements are set.
                // If required roles/permissions are provided, caller should specify the route param name to avoid accidental tenant-based authorization bypass.
                if (_requiredRoles.Length == 0 && _requiredPermissions.Length == 0)
                {
                    if (routeValues.TryGetValue("companyId", out var v2) && v2 != null && int.TryParse(v2.ToString(), out var p2)) resourceCompanyId = p2;
                    else if (routeValues.TryGetValue("id", out var v3) && v3 != null && int.TryParse(v3.ToString(), out var p3)) resourceCompanyId = p3;
                }
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

            var requiredRoles = _requiredRoles.Length > 0 ? _requiredRoles : null;
            var requiredPermissions = _requiredPermissions.Length > 0 ? _requiredPermissions : null;

            var ok = _auth.AuthorizeAsync(user!, requiredRoles, requiredPermissions, resourceCompanyId).Result;
            return ok;
        }
    }
}
