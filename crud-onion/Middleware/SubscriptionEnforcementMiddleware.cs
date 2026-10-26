using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain.Billing;
using System.Security.Claims;
using System.Text.Json;

namespace Onion.Middleware
{
    public sealed class SubscriptionEnforcementMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;

        public SubscriptionEnforcementMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context, ISubscriptionService subscriptionService, IUnitOfWork uow)
        {
            if (context.User.Identity?.IsAuthenticated != true || IsExcluded(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Allow read-only operations (GET, HEAD, OPTIONS) so tenants can view reports/invoices
            var method = context.Request.Method.ToUpperInvariant();
            if (method == "GET" || method == "HEAD" || method == "OPTIONS")
            {
                await _next(context);
                return;
            }

            // Bypass check for SuperAdmin
            if (context.User.IsInRole("SuperAdmin") || context.User.FindFirst("isSuperUser")?.Value == "true")
            {
                await _next(context);
                return;
            }

            // Extract CompanyId
            var companyIdClaim = context.User.FindFirst("CompanyId")?.Value
                ?? context.User.FindFirst("companyId")?.Value
                ?? context.User.FindFirst(ClaimTypes.GroupSid)?.Value;

            if (!int.TryParse(companyIdClaim, out var companyId) || companyId <= 0)
            {
                await _next(context);
                return;
            }

            var cacheKey = $"company_sub_active_{companyId}";
            var isWriteAllowed = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);

                var company = await uow.Companies.GetByIdAsync(companyId);
                if (company == null || !company.Active || company.IsDeleted)
                {
                    return false;
                }

                var sub = await subscriptionService.GetCompanySubscriptionAsync(companyId);
                if (sub == null)
                {
                    return true;
                }

                return sub.Status == SubscriptionStatus.Active || sub.Status == SubscriptionStatus.Trialing;
            });

            if (!isWriteAllowed)
            {
                context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    success = false,
                    code = "SUBSCRIPTION_PAYMENT_REQUIRED",
                    message = "El acceso de escritura de su empresa ha sido suspendido debido a falta de pago de la suscripción SaaS. Por favor, regularice su balance en el portal de facturación o contacte a soporte de CuadreEnv.",
                    companyId,
                    requestId = context.TraceIdentifier
                }));
                return;
            }

            await _next(context);
        }

        private static bool IsExcluded(PathString path) =>
            path.StartsWithSegments("/v1/auth") ||
            path.StartsWithSegments("/auth") ||
            path.StartsWithSegments("/Subscription") ||
            path.StartsWithSegments("/api/subscription") ||
            path.StartsWithSegments("/Company") ||
            path.StartsWithSegments("/api/Company") ||
            path.StartsWithSegments("/hc") ||
            path.StartsWithSegments("/swagger") ||
            path == "/";
    }
}
