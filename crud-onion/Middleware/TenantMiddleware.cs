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
            var path = context.Request.Path.HasValue ? context.Request.Path.Value ?? string.Empty : string.Empty;
            var method = context.Request.Method ?? string.Empty;

            var normalizedPath = path.TrimEnd('/');

            if (normalizedPath.StartsWith("/auth", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.EndsWith("/users/my-companies", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.StartsWith("/v1/auth/", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.Equals("/health", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.Equals("/hc", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (user?.Identity != null && user.Identity.IsAuthenticated)
            {
                var userIdClaim = user.FindFirst("sub") ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                _ = int.TryParse(userIdClaim?.Value, out var userId);
                var requestedCompanyId = ResolveCompanyId(context);

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

                var hasCompanyAccess = (userRecord != null && userRecord.CompanyId == resolvedCompanyId.Value) ||
                    (userId > 0 && await db.UserCompanies.IgnoreQueryFilters().AsNoTracking()
                        .AnyAsync(x => x.UserId == userId && x.CompanyId == resolvedCompanyId.Value && x.Active && !x.IsDeleted)) ||
                    IsGlobalAdministratorClaim(user);
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

        private static int? ResolveCompanyId(HttpContext context)
        {
            if (context.Items.TryGetValue("TenantCompanyId", out var itemValue) &&
                TryParseCompanyId(itemValue, out var itemCompanyId))
                return itemCompanyId;

            var headerValue = context.Request.Headers["X-Company-Id"].FirstOrDefault()
                ?? context.Request.Headers["x-company-id"].FirstOrDefault();
            if (TryParseCompanyId(headerValue, out var headerCompanyId))
                return headerCompanyId;

            var claimValue = context.User.FindFirst("company_id")?.Value
                ?? context.User.FindFirst("companyId")?.Value
                ?? context.User.FindFirst("CompanyId")?.Value;
            if (TryParseCompanyId(claimValue, out var claimCompanyId))
                return claimCompanyId;

            return null;
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
