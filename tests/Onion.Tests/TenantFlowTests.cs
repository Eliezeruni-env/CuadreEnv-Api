using System.Security.Claims;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Onion.Common.Services;
using Onion.Controllers.Middleware;
using Xunit;

namespace Onion.Tests;

public class TenantFlowTests
{
    [Fact]
    public async Task Middleware_AllowsAuthenticatedUserWithTenant()
    {
        var context = CreateContext("/v1/sale", "GET", new Claim("companyId", "12"));
        var called = false;
        var middleware = new TenantMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(called);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task Middleware_RejectsAuthenticatedUserWithoutTenant()
    {
        var context = CreateContext("/v1/sale", "GET");
        var middleware = new TenantMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Contains("COMPANY_REQUIRED", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task Middleware_AllowsCompanyCreationWithoutTenant()
    {
        var context = CreateContext("/v1/company", "POST");
        var called = false;
        var middleware = new TenantMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(called);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task Middleware_AllowsSuperUserWithoutTenant()
    {
        var context = CreateContext("/v1/sale", "GET", new Claim("isSuperUser", "true"));
        var called = false;
        var middleware = new TenantMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(called);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public void CurrentUserService_RecognizesBothTenantClaimNamesAndSuperUser()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("CompanyId", "7"),
            new Claim("isSuperUser", "true")
        }, "test"));
        var service = new CurrentUserService(
            new HttpContextAccessor { HttpContext = httpContext },
            new ConfigurationBuilder().Build());

        Assert.Equal(7, service.CompanyId);
        Assert.True(service.IsGlobalAdministrator);
    }

    private static DefaultHttpContext CreateContext(string path, string method, params Claim[] claims)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = path;
        context.Request.Method = method;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}
