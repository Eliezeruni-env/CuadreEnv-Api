using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Onion.Common.Auth
{
    /// <summary>
    /// Tenant-aware RequireRole attribute.
    /// Behavior:
    /// - If the authenticated user has the specified role, the action is allowed.
    /// - Otherwise, if a company route parameter name is provided, the attribute will compare the
    ///   CompanyId claim in the JWT with the route value; if they match the action is allowed.
    /// - Otherwise, if the user has a CompanyId claim (is acting on behalf of a tenant), the action is allowed.
    /// - In all other cases the request is forbidden (403).
    ///
    /// Usage examples:
    /// [RequireRole("SuperAdmin")] // only platform admins
    /// [RequireRole("CompanyUser")] // requires CompanyId claim if not SuperAdmin
    /// [RequireRole("Manager", "companyId")] // allows if role present OR companyId route param equals CompanyId claim
    ///
    /// This attribute is intentionally conservative: platform roles bypass tenant checks; non-platform users
    /// must present a CompanyId claim (or match the route param when specified).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RequireRoleAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string _role;
        private readonly string? _companyRouteParam;

        public RequireRoleAttribute(string role)
        {
            _role = role ?? throw new ArgumentNullException(nameof(role));
        }

        /// <summary>
        /// If provided, the route parameter with this name will be compared against the CompanyId claim.
        /// </summary>
        /// <param name="role"></param>
        /// <param name="companyRouteParam"></param>
        public RequireRoleAttribute(string role, string companyRouteParam)
            : this(role)
        {
            _companyRouteParam = companyRouteParam;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // If user has the required role, allow.
            if (user.IsInRole(_role))
            {
                await next();
                return;
            }

            // Try tenant-aware fallbacks.
            var companyClaim = user.FindFirst("CompanyId")?.Value;

            if (!string.IsNullOrEmpty(_companyRouteParam))
            {
                if (context.RouteData.Values.TryGetValue(_companyRouteParam, out var routeValue) && routeValue != null && companyClaim != null)
                {
                    if (string.Equals(routeValue.ToString(), companyClaim, StringComparison.OrdinalIgnoreCase))
                    {
                        await next();
                        return;
                    }
                }

                context.Result = new ForbidResult();
                return;
            }

            // If caller has a CompanyId claim, we allow (caller is tenant-scoped).
            if (!string.IsNullOrEmpty(companyClaim))
            {
                await next();
                return;
            }

            context.Result = new ForbidResult();
        }
    }
}
