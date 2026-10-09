using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Onion.Common.Models;
using System.Text.Json;

namespace Onion.Controllers.Middleware
{
    // Middleware to ensure authenticated requests include a CompanyId claim
    // so multi-tenant scoping can be enforced centrally by the tenant provider and DbContext filters.
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var user = context.User;
            // Allow some public endpoints to be called without tenant claim even if caller is authenticated
            var path = context.Request.Path.HasValue ? context.Request.Path.Value ?? string.Empty : string.Empty;
            var method = context.Request.Method ?? string.Empty;

            // POST /company is used to register a new tenant; authenticated callers without a CompanyId
            // should be allowed to create a company and then be associated to it. Skip tenant validation
            // for that specific case.
            var normalizedPath = path.TrimEnd('/');
            var pathSegments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var resource = pathSegments.Length > 0 ? pathSegments[^1] : string.Empty;
            var parentResource = pathSegments.Length > 1 ? pathSegments[^2] : string.Empty;

            // Authentication and company-plan discovery are valid before onboarding.
            if (resource.Equals("auth", StringComparison.OrdinalIgnoreCase) ||
                (parentResource.Equals("company", StringComparison.OrdinalIgnoreCase) &&
                 resource.Equals("plans", StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            if ((string.Equals(normalizedPath, "/company", System.StringComparison.OrdinalIgnoreCase) ||
                 normalizedPath.EndsWith("/company", System.StringComparison.OrdinalIgnoreCase)) &&
                string.Equals(method, "POST", System.StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (user?.Identity != null && user.Identity.IsAuthenticated)
            {
                var currentUser = context.RequestServices?.GetService<Onion.Common.Services.ICurrentUserService>();
                var isGlobalAdministrator = currentUser?.IsGlobalAdministrator == true || IsGlobalAdministratorClaim(user);
                if (isGlobalAdministrator)
                {
                    await _next(context);
                    return;
                }

                var claim = user.FindFirst("companyId") ?? user.FindFirst("CompanyId");
                if (claim == null || string.IsNullOrWhiteSpace(claim.Value))
                {
                    await WriteCompanyRequiredAsync(context);
                    return;
                }

                // Reject non-positive company ids (0 or negative) as invalid tenant context
                if (!int.TryParse(claim.Value, out var cid) || cid <= 0)
                {
                    await WriteCompanyRequiredAsync(context);
                    return;
                }
            }

            await _next(context);
        }

        private static bool IsGlobalAdministratorClaim(System.Security.Claims.ClaimsPrincipal user)
        {
            var isSuperUser = user.Claims.Any(c =>
                string.Equals(c.Type, "isSuperUser", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.Value, "true", StringComparison.OrdinalIgnoreCase));

            if (isSuperUser) return true;

            var role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                ?? user.FindFirst("role")?.Value;

            return string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, "SysAdmin", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task WriteCompanyRequiredAsync(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var response = new
            {
                errorCode = "COMPANY_REQUIRED",
                message = "El usuario no tiene una empresa asignada.",
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

    public static class TenantMiddlewareExtensions
    {
        public static IApplicationBuilder UseTenantValidation(this IApplicationBuilder app)
        {
            return app.UseMiddleware<TenantMiddleware>();
        }
    }
}
