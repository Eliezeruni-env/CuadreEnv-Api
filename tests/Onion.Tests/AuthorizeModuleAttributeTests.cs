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
        var service = new StubEntitlements(new CompanyEntitlements(new HashSet<string> { "ECF" }, new HashSet<string>()));
        var filter = new AuthorizeModuleFilter(service, "ECF");
        var context = CreateContext(7);
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
        var filter = new AuthorizeModuleFilter(service, "ECF");
        var context = CreateContext(7);

        await filter.OnActionExecutionAsync(context, () => throw new InvalidOperationException("next must not execute"));

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
        Assert.Contains("MODULE_NOT_LICENSED", result.Value?.ToString());
    }

    private static ActionExecutingContext CreateContext(int companyId)
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim("companyId", companyId.ToString())
        }, "test"));
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
