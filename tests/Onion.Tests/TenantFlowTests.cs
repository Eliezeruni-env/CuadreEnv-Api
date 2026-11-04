using System.Security.Claims;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Onion.DataAccess;
using Onion.Domain;
using Onion.Common.Services;
using Onion.Controllers.Middleware;
using Xunit;

namespace Onion.Tests;

public class TenantFlowTests
{
    [Fact]
    public async Task Middleware_AllowsAuthenticatedUserWithTenant()
    {
        var context = CreateContext("/v1/sale", "GET", new Claim("sub", "1"), new Claim("companyId", "12"), new Claim("isSuperUser", "true"));
        var called = false;
        var middleware = new TenantMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(called, await ReadBodyAsync(context));
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task Middleware_RejectsAuthenticatedUserWithoutTenant()
    {
        var context = CreateContext("/v1/sale", "GET");
        var middleware = new TenantMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains("COMPANY_CONTEXT_REQUIRED", await ReadBodyAsync(context));
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
    public async Task Middleware_RejectsSuperUserWithoutTenant()
    {
        var context = CreateContext("/v1/sale", "GET", new Claim("isSuperUser", "true"));
        var middleware = new TenantMiddleware(_ =>
        {
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains("COMPANY_CONTEXT_REQUIRED", await ReadBodyAsync(context));
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

    [Fact]
    public void JwtTenantProvider_PrioritizesItemsOverHeaderAndClaim()
    {
        var context = new DefaultHttpContext();
        context.Items["TenantCompanyId"] = "12";
        context.Request.Headers["X-Company-Id"] = "20";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("companyId", "30") }, "test"));

        var provider = new Onion.DataAccess.Tenant.JwtTenantProvider(new HttpContextAccessor { HttpContext = context });

        Assert.Equal(12, provider.GetCompanyId());
    }

    [Fact]
    public void JwtTenantProvider_PrioritizesHeaderOverClaim()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["x-company-id"] = "20";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("CompanyId", "30") }, "test"));

        var provider = new Onion.DataAccess.Tenant.JwtTenantProvider(new HttpContextAccessor { HttpContext = context });

        Assert.Equal(20, provider.GetCompanyId());
    }

    [Fact]
    public async Task Middleware_PrioritizesExistingTenantItem()
    {
        var context = CreateContext("/v1/sale", "GET", new Claim("sub", "1"), new Claim("companyId", "12"));
        context.Items["TenantCompanyId"] = 12;
        context.Request.Headers["X-Company-Id"] = "999";
        var called = false;
        var middleware = new TenantMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(called, await ReadBodyAsync(context));
        Assert.Equal(12, context.Items["TenantCompanyId"]);
    }

    private static DefaultHttpContext CreateContext(string path, string method, params Claim[] claims)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = path;
        context.Request.Method = method;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        if (claims.Any(c => c.Type == "companyId" && c.Value == "12"))
        {
            var services = new ServiceCollection();
            services.AddSingleton<Onion.Common.Services.ICurrentUserService>(new TestCurrentUserService(12));
            services.AddDbContext<OnionDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")), ServiceLifetime.Singleton);
            var provider = services.BuildServiceProvider();
            var db = provider.GetRequiredService<OnionDbContext>();
            db.Companies.Add(new Company { Id = 12, Name = "Test Company", Active = true, IsDeleted = false });
            db.UserCompanies.Add(new Onion.Domain.Users.UserCompany
            {
                UserId = 1,
                CompanyId = 12,
                Active = true,
                IsDeleted = false
            });
            db.Users.Add(new Onion.Domain.Users.User
            {
                Id = 1,
                CompanyId = 12,
                FirstName = "Test",
                LastName = "User",
                Gender = "M",
                Email = "test@example.com",
                PasswordHash = "test",
                PhoneNumber = "0000000000",
                UserName = "test-user",
                CreateBy = "test"
            });
            db.SaveChanges();
            if (!db.UserCompanies.IgnoreQueryFilters().Any(x => x.UserId == 1 && x.CompanyId == 12 && x.Active && !x.IsDeleted))
                throw new InvalidOperationException("Tenant test fixture membership was not persisted.");
            context.RequestServices = provider;
        }
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    private sealed class TestCurrentUserService(int companyId) : Onion.Common.Services.ICurrentUserService
    {
        public int? CompanyId => companyId;
        public int? UserId => 1;
        public string? ClerkUserId => "1";
        public string? UserEmail => "test@example.com";
        public string? UserRole => "Admin";
        public string? IpAddress => null;
        public string? UserAgent => null;
        public bool IsAuthenticated => true;
        public bool IsGlobalAdministrator => false;
    }
}
