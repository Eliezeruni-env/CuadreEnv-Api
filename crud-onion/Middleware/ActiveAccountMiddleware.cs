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
        if (!int.TryParse(idValue, out var userId) || !await accountStatus.IsActiveAsync(userId, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                success = false,
                code = "ACCOUNT_SUSPENDED",
                message = "Su cuenta se encuentra suspendida o deshabilitada. Comuníquese con la administración de CuadreEnv."
            }));
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
}
