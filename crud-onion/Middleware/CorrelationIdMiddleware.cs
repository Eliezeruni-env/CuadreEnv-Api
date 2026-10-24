using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace crud_onion.Middleware;

public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetCorrelationId(context.Request.Headers[HeaderName].FirstOrDefault());
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }

    private static string GetCorrelationId(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested) && requested.Length <= 128 &&
            requested.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.'))
        {
            return requested;
        }

        return Guid.NewGuid().ToString("N");
    }
}
