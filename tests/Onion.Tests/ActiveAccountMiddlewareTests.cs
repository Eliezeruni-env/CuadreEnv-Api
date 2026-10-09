using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Middleware;
using System.Security.Claims;
using Xunit;

namespace Onion.Tests;

public sealed class ActiveAccountMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_RejectsMissingUserAsInactiveAccount()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "999") }, "Bearer"))
        };
        context.Request.Path = "/v1/cashregister";
        context.Response.Body = new MemoryStream();
        var nextCalled = false;
        var middleware = new ActiveAccountMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, new MemoryCache(new MemoryCacheOptions()));

        await middleware.InvokeAsync(context, new StubAccountStatusService(false));

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.Contains("ACCOUNT_SUSPENDED", body);
    }

    private sealed class StubAccountStatusService : IAccountStatusService
    {
        private readonly bool _active;

        public StubAccountStatusService(bool active) => _active = active;

        public Task<bool> IsActiveAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_active);
    }
}
