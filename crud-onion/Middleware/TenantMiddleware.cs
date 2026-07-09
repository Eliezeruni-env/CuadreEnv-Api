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
            if (string.Equals(path, "/company", System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(method, "POST", System.StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (user?.Identity != null && user.Identity.IsAuthenticated)
            {
                var claim = user.FindFirst("CompanyId");
                if (claim == null || string.IsNullOrWhiteSpace(claim.Value))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";
                    var resp = ApiResponse<object>.Fail("CompanyId claim missing or invalid", new[] { "MISSING_COMPANY_CLAIM" });
                    await context.Response.WriteAsync(JsonSerializer.Serialize(resp));
                    return;
                }

                // Reject non-positive company ids (0 or negative) as invalid tenant context
                if (!int.TryParse(claim.Value, out var cid) || cid <= 0)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";
                    var resp = ApiResponse<object>.Fail("CompanyId claim missing or invalid", new[] { "MISSING_COMPANY_CLAIM" });
                    await context.Response.WriteAsync(JsonSerializer.Serialize(resp));
                    return;
                }
            }

            await _next(context);
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
