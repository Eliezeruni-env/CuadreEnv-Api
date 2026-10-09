using Microsoft.Extensions.Options;
using crud_onion.Authentication;

namespace crud_onion.Middleware;

public sealed class ClerkAccessMiddleware(RequestDelegate next, IOptions<ClerkOptions> options)
{
    private readonly ClerkOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true || IsAnonymousEndpoint(context))
        {
            await next(context);
            return;
        }

        var hasConfiguredAccess = !string.IsNullOrWhiteSpace(_options.AccessClaim);
        var accessClaim = hasConfiguredAccess ? context.User.FindFirst(_options.AccessClaim!) : null;
        var hasExplicitAccess = accessClaim is not null &&
            string.Equals(accessClaim.Value, _options.AccessClaimExpectedValue, StringComparison.OrdinalIgnoreCase);
        var hasOrganization = !string.IsNullOrWhiteSpace(context.User.FindFirst("org_id")?.Value);
        var hasProjectAccess = context.User.Claims
            .Where(claim => claim.Type is "projects" or "publicMetadata" or "public_metadata" or "publicMetadata.projects" or "public_metadata.projects")
            .Any(claim => !string.IsNullOrWhiteSpace(claim.Value));

        // USM may grant access explicitly through metadata, or implicitly through
        // membership in a SaaS organization. Do not require both mechanisms.
        var hasSaasAccess = hasExplicitAccess || hasOrganization || hasProjectAccess;
        if (!hasSaasAccess)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { errorCode = "SAAS_ACCESS_REQUIRED", message = "El usuario no tiene acceso a este SaaS." });
            return;
        }

        await next(context);
    }

    private static bool IsAnonymousEndpoint(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/hc") ||
        context.Request.Path.StartsWithSegments("/api/webhooks/clerk") ||
        context.Request.Path.StartsWithSegments("/v1/api/webhooks/clerk") ||
        context.Request.Path == "/";
}
