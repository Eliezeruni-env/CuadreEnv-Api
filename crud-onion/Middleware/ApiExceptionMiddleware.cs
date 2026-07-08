using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Onion.Common.Exceptions;
using Onion.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Onion.Controllers.Middleware
{
    public class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;
        private readonly ILogger<ApiExceptionMiddleware> _logger;

        public ApiExceptionMiddleware(RequestDelegate next, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env, ILogger<ApiExceptionMiddleware> logger)
        {
            _next = next;
            _env = env;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Onion.Common.Exceptions.HttpResponseException hex)
            {
                var status = (int)hex.StatusCode;
                _logger.LogWarning("Handled HttpResponseException: {Status}", status);

                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { statusCode = status, errors = hex.Errors }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
                return;
            }
            catch (CustomException cex)
            {
                int status;
                // Map known error codes to HTTP statuses
                if (cex.Error.Code == "NOT_FOUND")
                    status = StatusCodes.Status404NotFound;
                else if (cex.Error.Code == "FORBIDDEN")
                    status = StatusCodes.Status403Forbidden;
                else
                    status = StatusCodes.Status400BadRequest;

                _logger.LogWarning("Handled CustomException: {Code} {Message}", cex.Error.Code, cex.Error.Message);

                var payload = new
                {
                    statusCode = status,
                    message = cex.Error.Message,
                    errorCode = cex.Error.Code,
                    timestamp = DateTime.UtcNow
                };

                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
                return;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception processing request {Path}", context.Request?.Path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var message = _env.IsDevelopment() ? "An unexpected error occurred" : "An internal server error occurred";
                var errorCode = _env.IsDevelopment() ? ex.Message : "INTERNAL_ERROR";

                var payload = new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = message,
                    errorCode = errorCode,
                    timestamp = DateTime.UtcNow
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            }
        }
    }


    public static class ApiExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ApiExceptionMiddleware>();
        }
    }
}
