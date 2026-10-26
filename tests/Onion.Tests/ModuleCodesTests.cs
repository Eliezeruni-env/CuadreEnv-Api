using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Onion.Common.Authorization;
using Onion.Common.Services;
using Xunit;

namespace Onion.Tests;

public sealed class ModuleCodesTests
{
    [Fact]
    public void FromClaimPreservingIds_Keeps_Granular_Codes_Literal()
    {
        var value = JsonSerializer.Serialize(new[] { "inventory-entries", "purchases-receipts", "inventory-stock", "payments" });

        var result = ModuleCodes.FromClaimPreservingIds(value);

        Assert.Equal(new[] { "inventory-entries", "purchases-receipts", "inventory-stock", "payments" }, result);
        Assert.Equal("inventory", ModuleCodes.Normalize("inventory-entries"));
        Assert.Equal("purchases", ModuleCodes.Normalize("purchases-receipts"));
    }

    [Fact]
    public async Task Exact_Filter_Rejects_Parent_Module_Without_Granular_Assignment()
    {
        var entitlements = new StubEntitlements(new CompanyEntitlements(
            new HashSet<string> { "inventory" }, new HashSet<string>()));
        var filter = new AuthorizeExactModuleFilter(entitlements, "inventory-entries");
        var context = CreateContext("inventory");

        await filter.OnActionExecutionAsync(context, () => throw new System.InvalidOperationException("next must not execute"));

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
    }

    private static ActionExecutingContext CreateContext(params string[] modules)
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("sub", "1"),
                new Claim("companyId", "7"),
                new Claim("allowedModules", JsonSerializer.Serialize(modules))
            }, "test"))
        };
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
