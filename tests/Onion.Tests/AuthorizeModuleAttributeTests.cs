using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Onion.Common.Authorization;
using Onion.Common.Services;
using Xunit;
using Onion.Controllers;
using System.Reflection;
using System.Text.Json;

namespace Onion.Tests;

public sealed class AuthorizeModuleAttributeTests
{
    [Theory]
    [InlineData(typeof(InventoryController), "INVENTORY")]
    [InlineData(typeof(SaleController), "SALES")]
    [InlineData(typeof(CajaController), "POS")]
    [InlineData(typeof(CashRegisterController), "POS")]
    [InlineData(typeof(PurchaseController), "PURCHASES")]
    [InlineData(typeof(ReportsController), "REPORTS")]
    public void Operational_Controller_Declares_Usm_Module(Type controllerType, string expectedModule)
    {
        var attribute = controllerType.GetCustomAttribute<AuthorizeModuleAttribute>();

        Assert.NotNull(attribute);
        var moduleCode = typeof(AuthorizeModuleAttribute)
            .GetProperty("Arguments", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?
            .GetValue(attribute) as object[];

        Assert.Contains(expectedModule, moduleCode ?? Array.Empty<object>());
    }

    [Fact]
    public async Task Allows_When_Module_Is_Licensed()
    {
        var service = new StubEntitlements(new CompanyEntitlements(new HashSet<string> { "sales" }, new HashSet<string>()));
        var filter = new AuthorizeModuleFilter(service, "sales");
        var context = CreateContext(7, allowedModules: "sales");
        var called = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult<ActionExecutedContext>(new ActionExecutedContext(context, Array.Empty<IFilterMetadata>(), null));
        });

        Assert.True(called);
        Assert.Null(context.Result);
    }

    [Theory]
    [InlineData("audit", "fraud-guardian")]
    [InlineData("company", "admin-roles")]
    public async Task Allows_Wildcard_User_When_Company_Entitlement_Uses_Catalog_Module_Id(string requestedModule, string licensedModuleId)
    {
        var service = new StubEntitlements(new CompanyEntitlements(new HashSet<string> { licensedModuleId }, new HashSet<string>()));
        var filter = new AuthorizeModuleFilter(service, requestedModule);
        var context = CreateContext(7, allowedModules: "*");
        var called = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult<ActionExecutedContext>(new ActionExecutedContext(context, Array.Empty<IFilterMetadata>(), null));
        });

        Assert.True(called);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task Returns_403_When_Module_Is_Not_Licensed()
    {
        var service = new StubEntitlements(new CompanyEntitlements(new HashSet<string>(), new HashSet<string>()));
        var filter = new AuthorizeModuleFilter(service, "sales");
        var context = CreateContext(7, allowedModules: "sales");

        await filter.OnActionExecutionAsync(context, () => throw new InvalidOperationException("next must not execute"));

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
        Assert.Contains("MODULE_NOT_LICENSED", result.Value?.ToString());
    }

    [Fact]
    public async Task Tenant_Admin_Uses_Tenant_Module_Authorization_Not_Global_Bypass()
    {
        var service = new StubEntitlements(new CompanyEntitlements(new HashSet<string>(), new HashSet<string>()));
        var filter = new AuthorizeModuleFilter(service, "sales");
        var context = CreateContext(7, "Admin", "sales");

        await filter.OnActionExecutionAsync(context, () => throw new InvalidOperationException("next must not execute"));

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
        Assert.Contains("MODULE_NOT_LICENSED", result.Value?.ToString());
    }

    [Fact]
    public async Task Tenant_Admin_Is_Denied_When_Module_Is_Not_Explicitly_Assigned()
    {
        var service = new StubEntitlements(new CompanyEntitlements(new HashSet<string> { "sales" }, new HashSet<string>()));
        var filter = new AuthorizeModuleFilter(service, "sales");
        var context = CreateContext(7, "Admin");

        await filter.OnActionExecutionAsync(context, () => throw new InvalidOperationException("next must not execute"));

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
        Assert.Contains("MODULE_NOT_ASSIGNED", result.Value?.ToString());
    }

    [Fact]
    public async Task Allows_Read_Only_Module_Dependency_When_Any_Assigned_Module_Is_Licensed()
    {
        var service = new StubEntitlements(new CompanyEntitlements(new HashSet<string> { "sales" }, new HashSet<string>()));
        var filter = new AuthorizeAnyModuleFilter(service, new[] { "inventory", "sales", "purchases" });
        var context = CreateContext(7, "Employee", "sales");
        var called = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult<ActionExecutedContext>(new ActionExecutedContext(context, Array.Empty<IFilterMetadata>(), null));
        });

        Assert.True(called);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task Denies_Read_Only_Module_Dependency_When_User_Has_No_Assigned_Module()
    {
        var service = new StubEntitlements(new CompanyEntitlements(new HashSet<string> { "sales" }, new HashSet<string>()));
        var filter = new AuthorizeAnyModuleFilter(service, new[] { "inventory", "sales", "purchases" });
        var context = CreateContext(7, "Employee", "customers");

        await filter.OnActionExecutionAsync(context, () => throw new InvalidOperationException("next must not execute"));

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
        Assert.Contains("MODULE_NOT_ASSIGNED", result.Value?.ToString());
    }

    private static ActionExecutingContext CreateContext(int companyId, string? role = null, params string[] allowedModules)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new("companyId", companyId.ToString()),
            new("role", role ?? "Employee")
        };
        if (allowedModules.Length > 0)
            claims.Add(new Claim("allowedModules", JsonSerializer.Serialize(allowedModules)));

        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        return new ActionExecutingContext(new ActionContext(http, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()), new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
    }

    private sealed class StubEntitlements : ICompanyEntitlementService
    {
        private readonly CompanyEntitlements _value;
        public StubEntitlements(CompanyEntitlements value) => _value = value;
        public Task<CompanyEntitlements> GetAsync(int companyId, CancellationToken cancellationToken = default) => Task.FromResult(_value);
        public void Invalidate(int companyId) { }
    }
}
