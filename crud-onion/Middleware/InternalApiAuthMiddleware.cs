using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Onion.Controllers.Middleware
{
    // Middleware to protect internal endpoints under /internal
    public class InternalApiAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string? _apiKey;

        public InternalApiAuthMiddleware(RequestDelegate next, IConfiguration config)
        {
            _next = next;
            _apiKey = config["Internal:ApiKey"];
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only apply to internal routes
            var path = context.Request.Path.Value ?? string.Empty;
            if (!path.StartsWith("/internal"))
            {
                await _next(context);
                return;
            }

            // 1) If an API key is configured, accept requests that present it in X-Internal-ApiKey
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                if (context.Request.Headers.TryGetValue("X-Internal-ApiKey", out var provided) &&
                    FixedTimeEquals(provided.ToString(), _apiKey))
                {
                    await _next(context);
                    return;
                }
            }

            // 2) Otherwise allow only authenticated users with SuperAdmin role
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                if (context.User.IsInRole("SuperAdmin") || context.User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "SuperAdmin"))
                {
                    await _next(context);
                    return;
                }
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
        }

        private static bool FixedTimeEquals(string provided, string expected)
        {
            var providedBytes = Encoding.UTF8.GetBytes(provided);
            var expectedBytes = Encoding.UTF8.GetBytes(expected);
            return providedBytes.Length == expectedBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
        }
    }
}
