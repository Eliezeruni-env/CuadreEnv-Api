using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Onion.BussinesLogic.Services.Abstract;
using System.Security.Claims;
using System.Text.Json;

namespace Onion.Middleware;

public sealed class ActiveAccountMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    public ActiveAccountMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, IAccountStatusService accountStatus)
    {
        if (context.User.Identity?.IsAuthenticated != true || IsExcluded(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var idValue = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst("userId")?.Value;
        if (!int.TryParse(idValue, out var userId))
        {
            await WriteIdentityErrorAsync(context, "INVALID_USER_TOKEN", "El token autenticado no contiene un usuario válido.", StatusCodes.Status401Unauthorized);
            return;
        }

        var cacheKey = $"user_active_{userId}";
        var accountExistsAndIsActive = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return await accountStatus.IsActiveAsync(userId, context.RequestAborted);
        });
        if (!accountExistsAndIsActive)
        {
            await WriteIdentityErrorAsync(context, "ACCOUNT_SUSPENDED", "Su cuenta se encuentra suspendida, deshabilitada o ya no existe. Inicie sesión nuevamente o comuníquese con la administración de CuadreEnv.", StatusCodes.Status403Forbidden);
            return;
        }

        await _next(context);
    }

    private static bool IsExcluded(PathString path) =>
        path.StartsWithSegments("/v1/auth/login") ||
        path.StartsWithSegments("/v1/auth/register") ||
        path.StartsWithSegments("/v1/auth/refresh") ||
        path.StartsWithSegments("/v1/auth/revoke") ||
        path.StartsWithSegments("/hc") ||
        path == "/";

    private static async Task WriteIdentityErrorAsync(HttpContext context, string code, string message, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            success = false,
            code,
            message,
            requestId = context.TraceIdentifier
        }));
    }
}
