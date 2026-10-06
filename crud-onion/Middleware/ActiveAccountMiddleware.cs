using Microsoft.AspNetCore.Http;
using Onion.BussinesLogic.Services.Abstract;
using System.Security.Claims;
using System.Text.Json;

namespace Onion.Middleware;

public sealed class ActiveAccountMiddleware
{
    private readonly RequestDelegate _next;
    public ActiveAccountMiddleware(RequestDelegate next) => _next = next;

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

        var accountExistsAndIsActive = await accountStatus.IsActiveAsync(userId, context.RequestAborted);
        if (!accountExistsAndIsActive)
        {
            await WriteIdentityErrorAsync(context, "ACCOUNT_SUSPENDED", "Su cuenta se encuentra suspendida, deshabilitada o ya no existe. Inicie sesión nuevamente o comuníquese con la administración de CuadreEnv.", StatusCodes.Status403Forbidden);
            return;
        }

        await _next(context);
    }

    private static bool IsExcluded(PathString path) =>
        path.StartsWithSegments("/auth/login") ||
        path.StartsWithSegments("/auth/register") ||
        path.StartsWithSegments("/auth/refresh") ||
        path.StartsWithSegments("/auth/revoke") ||
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
