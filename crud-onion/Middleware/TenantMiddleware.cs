using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Onion.Common.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

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
            if (normalizedPath.EndsWith("/users/my-companies", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.EndsWith("/auth/switch-company", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
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

                var userIdClaim = user.FindFirst("sub") ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                _ = int.TryParse(userIdClaim?.Value, out var userId);
                var headerValue = context.Request.Headers["X-Company-Id"].FirstOrDefault();
                var claimValue = (user.FindFirst("companyId") ?? user.FindFirst("CompanyId"))?.Value;

                int? requestedCompanyId = null;
                if (int.TryParse(headerValue, out var headerCompanyId) && headerCompanyId > 0)
                    requestedCompanyId = headerCompanyId;
                else if (int.TryParse(claimValue, out var claimCompanyId) && claimCompanyId > 0)
                    requestedCompanyId = claimCompanyId;

                if (requestedCompanyId is not > 0 && userId <= 0)
                {
                    await WriteCompanyRequiredAsync(context);
                    return;
                }

                var db = context.RequestServices.GetRequiredService<Onion.DataAccess.OnionDbContext>();
                var userRecord = userId > 0
                    ? await db.Users.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId)
                    : null;
                var resolvedCompanyId = requestedCompanyId ?? userRecord?.CompanyId;

                if (resolvedCompanyId is not > 0)
                {
                    await WriteCompanyRequiredAsync(context);
                    return;
                }

                var companyExists = await db.Companies.IgnoreQueryFilters().AsNoTracking()
                    .AnyAsync(x => x.Id == resolvedCompanyId.Value && x.Active && !x.IsDeleted);
                if (!companyExists)
                {
                    await WriteCompanyRequiredAsync(context);
                    return;
                }

                var hasCompanyAccess = userRecord?.CompanyId == resolvedCompanyId.Value ||
                    (userId > 0 && await db.UserCompanies.IgnoreQueryFilters().AsNoTracking()
                        .AnyAsync(x => x.UserId == userId && x.CompanyId == resolvedCompanyId.Value && x.Active && !x.IsDeleted));
                if (!hasCompanyAccess && !isGlobalAdministrator)
                {
                    await WriteCompanyRequiredAsync(context);
                    return;
                }

                // The request tenant is now authoritative for CurrentUserService,
                // JwtTenantProvider and the EF global filters.
                context.Items["TenantCompanyId"] = resolvedCompanyId.Value;
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
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            var response = new
            {
                errorCode = "COMPANY_CONTEXT_REQUIRED",
                message = "Contexto de empresa no establecido. Seleccione una empresa o envíe un CompanyId válido.",
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
