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
            var pathSegments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var resource = pathSegments.Length > 0 ? pathSegments[^1] : string.Empty;
            var parentResource = pathSegments.Length > 1 ? pathSegments[^2] : string.Empty;

            // Authentication, switch-company, my-companies, and company-plan discovery are valid before/during onboarding and login.
            if (normalizedPath.Contains("/auth", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.EndsWith("/users/my-companies", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.Contains("/api/users/login-sync", StringComparison.OrdinalIgnoreCase) ||
                (parentResource.Equals("company", StringComparison.OrdinalIgnoreCase) &&
                 resource.Equals("plans", StringComparison.OrdinalIgnoreCase)))
            {
                var cid = ResolveCompanyId(context, user);
                if (cid.HasValue && cid.Value > 0)
                {
                    context.Items["TenantCompanyId"] = cid.Value;
                }
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
                var userIdClaim = user.FindFirst("sub") ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                _ = int.TryParse(userIdClaim?.Value, out var userId);
                var requestedCompanyId = ResolveCompanyId(context, user);

                if (userId <= 0)
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

                var hasCompanyAccess = userId > 0 && (
                    (userRecord != null && userRecord.CompanyId == resolvedCompanyId.Value) ||
                    await db.UserCompanies.IgnoreQueryFilters().AsNoTracking()
                        .AnyAsync(x => x.UserId == userId && x.CompanyId == resolvedCompanyId.Value && x.Active && !x.IsDeleted) ||
                    (userRecord != null && (userRecord.IsSuperUser || string.Equals(userRecord.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase)))
                );
                if (!hasCompanyAccess)
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

        private static int? ResolveCompanyId(HttpContext context, System.Security.Claims.ClaimsPrincipal user)
        {
            if (context.Items.TryGetValue("TenantCompanyId", out var itemValue) &&
                TryParseCompanyId(itemValue, out var itemCompanyId))
                return itemCompanyId;

            var headerValue = context.Request.Headers["X-Company-Id"].FirstOrDefault()
                ?? context.Request.Headers["x-company-id"].FirstOrDefault();
            if (TryParseCompanyId(headerValue, out var headerCompanyId))
                return headerCompanyId;

            var claimValue = (user.FindFirst("companyId") ?? user.FindFirst("CompanyId"))?.Value;
            return TryParseCompanyId(claimValue, out var claimCompanyId) ? claimCompanyId : null;
        }

        private static bool TryParseCompanyId(object? value, out int companyId)
        {
            return int.TryParse(value?.ToString(), out companyId) && companyId > 0;
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
