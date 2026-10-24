using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Onion.Common.Authorization;
using Onion.Common.Services;
using Onion.Controllers;
using Xunit;

namespace Onion.Tests;

public sealed class ProtectedAdminEndpointsTests
{
    [Theory]
    [InlineData(typeof(RoleController), "Get")]
    [InlineData(typeof(DeletionApprovalController), "GetAll")]
    [InlineData(typeof(UserManagementController), "GetAll")]
    public async Task Authenticated_Non_Admin_Receives_403(Type controllerType, string actionName)
    {
        var method = controllerType.GetMethod(actionName)!;
        var attribute = method.GetCustomAttributes(typeof(AuthorizeExactModuleAttribute), true)
            .Cast<AuthorizeExactModuleAttribute>()
            .FirstOrDefault()
            ?? controllerType.GetCustomAttributes(typeof(AuthorizeExactModuleAttribute), true)
                .Cast<AuthorizeExactModuleAttribute>()
                .First();
        var context = new ActionExecutingContext(
            new ActionContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.Role, RolesConstants.Employee) }, "Test"))
                },
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()
            },
            Array.Empty<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());

        var entitlement = new TestEntitlements();
        var filter = new AuthorizeExactModuleFilter(entitlement, "users");
        await filter.OnActionExecutionAsync(context, () =>
            throw new InvalidOperationException("A user without the required module must not reach the action."));

        Assert.IsType<ObjectResult>(context.Result);
    }

    private sealed class TestEntitlements : ICompanyEntitlementService
    {
        public Task<CompanyEntitlements> GetAsync(int companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CompanyEntitlements(new HashSet<string> { "company", "audit" }, new HashSet<string>()));

        public void Invalidate(int companyId) { }
    }
}
