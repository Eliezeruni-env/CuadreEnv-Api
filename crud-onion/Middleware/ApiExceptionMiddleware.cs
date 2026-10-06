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
            catch (Onion.Common.Exceptions.TenantRequiredException ex)
            {
                await WriteBusinessErrorAsync(context, StatusCodes.Status401Unauthorized, "TENANT_REQUIRED", ex.Message);
                return;
            }
            catch (Onion.Common.Exceptions.FiscalSequenceExhaustedException ex)
            {
                await WriteBusinessErrorAsync(context, StatusCodes.Status409Conflict, "FISCAL_SEQUENCE_EXHAUSTED", ex.Message);
                return;
            }
            catch (Onion.Common.Exceptions.FiscalSequenceExpiredException ex)
            {
                await WriteBusinessErrorAsync(context, StatusCodes.Status409Conflict, "FISCAL_SEQUENCE_EXPIRED", ex.Message);
                return;
            }
            catch (Onion.Common.Exceptions.InsufficientStockException ex)
            {
                await WriteBusinessErrorAsync(context, StatusCodes.Status409Conflict, "INSUFFICIENT_STOCK", ex.Message);
                return;
            }
            catch (Onion.Common.Exceptions.CashSessionException ex)
            {
                await WriteBusinessErrorAsync(context, ex.Code == "CASH_SESSION_NOT_FOUND" ? StatusCodes.Status404NotFound : ex.Code == "OUT_OF_TOLERANCE_CLOSING" ? StatusCodes.Status422UnprocessableEntity : StatusCodes.Status409Conflict, ex.Code, ex.Message);
                return;
            }
            catch (CustomException cex)
            {
                int status;
                // Map known error codes to HTTP statuses
                if (cex.Error.Code == "INVALID_TOKEN" || cex.Error.Code == "INVALID_USER_TOKEN")
                    status = StatusCodes.Status401Unauthorized;
                else if (cex.Error.Code == "NOT_FOUND")
                    status = StatusCodes.Status404NotFound;
                else if (cex.Error.Code == "FORBIDDEN")
                    status = StatusCodes.Status403Forbidden;
                else if (cex.Error.Code is "CASH_REGISTER_CHANGED" or "CONCURRENCY_CONFLICT")
                    status = StatusCodes.Status409Conflict;
                else
                    status = StatusCodes.Status400BadRequest;

                // Include request context to help FE/ops correlate errors
                var requestId = context.TraceIdentifier;
                _logger.LogWarning("Handled CustomException: {Code} {Message} RequestId={RequestId} Path={Path}", cex.Error.Code, cex.Error.Message, requestId, context.Request?.Path);

                var payload = new
                {
                    statusCode = status,
                    message = cex.Error.Message,
                    errorCode = cex.Error.Code,
                    requestId = requestId,
                    path = context.Request?.Path,
                    method = context.Request?.Method,
                    timestamp = DateTime.UtcNow,
                    // include structured details when present (e.g., field validation errors)
                    details = cex.Error.Details
                };

                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));
                return;
            }

            catch (System.Exception ex)
            {
                var requestId = context.TraceIdentifier;
                _logger.LogError(ex, "Unhandled exception processing request {Path} RequestId={RequestId} Method={Method}", context.Request?.Path, requestId, context.Request?.Method);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                // Surface minimal diagnostic info to FE in development only to avoid leaking secrets in production
                var payload = new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = _env.IsDevelopment() ? ex.Message : "An internal server error occurred",
                    errorCode = _env.IsDevelopment() ? ex.GetType().Name : "INTERNAL_ERROR",
                    requestId = requestId,
                    path = context.Request?.Path,
                    method = context.Request?.Method,
                    timestamp = DateTime.UtcNow,
                    // include stack trace and inner exception only in development
                    exception = _env.IsDevelopment() ? new { ex.Message, ex.StackTrace, inner = ex.InnerException?.Message } : null
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));
            }
        }

        private static async Task WriteBusinessErrorAsync(HttpContext context, int status, string code, string message)
        {
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            var payload = Onion.Common.Models.ApiResponse<object>.Fail(message, new[] { code });
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
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
