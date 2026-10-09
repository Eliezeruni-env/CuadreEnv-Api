using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Services;
using Onion.Controllers;
using Onion.Domain.Users;
using Xunit;

namespace Onion.Tests;

public sealed class AuthModulesContractTests
{
    [Fact]
    public async Task Returns_Empty_Lists_When_User_Has_No_Assignment_And_Company_Has_No_License()
    {
        var response = await GetModules("[]", Array.Empty<string>());

        Assert.Empty(response.GetProperty("assignedModules").EnumerateArray());
        Assert.Empty(response.GetProperty("licensedModules").EnumerateArray());
        Assert.Empty(response.GetProperty("modules").EnumerateArray());
    }

    [Fact]
    public async Task Limits_Assigned_Wildcard_To_Company_License()
    {
        var response = await GetModules("[\"*\"]", new[] { "company" });

        Assert.Equal(new[] { "*" }, Strings(response, "assignedModules"));
        Assert.Equal(new[] { "company" }, Strings(response, "licensedModules"));
        Assert.Equal(new[] { "company" }, Strings(response, "modules"));
    }

    [Fact]
    public async Task Preserves_Specific_Usm_Ids_While_Comparing_Normalized_License()
    {
        var response = await GetModules("[\"admin-roles\",\"fraud-guardian\"]", new[] { "company", "audit" });

        Assert.Equal(new[] { "admin-roles", "fraud-guardian" }, Strings(response, "assignedModules"));
        Assert.Equal(new[] { "admin-roles", "fraud-guardian" }, Strings(response, "modules"));
    }

    [Fact]
    public async Task Reads_Usm_Modules_Wrapped_In_Data_With_Module_Id()
    {
        var response = await GetModules(
            "{\"data\":[{\"moduleId\":\"fraud-guardian\"},{\"moduleId\":\"admin-roles\"}]}",
            new[] { "audit", "company" });

        Assert.Equal(new[] { "fraud-guardian", "admin-roles" }, Strings(response, "assignedModules"));
        Assert.Equal(new[] { "fraud-guardian", "admin-roles" }, Strings(response, "modules"));
    }

    [Fact]
    public async Task Returns_Current_Database_Assignment_Instead_Of_Stale_Token_Claims()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", "42"),
            new Claim("companyId", "7"),
            new Claim("allowedModules", "[\"billing\"]")
        }, "Test");
        var controller = new AuthController(null!)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
        var currentUser = new User
        {
            Id = 42,
            FirstName = "Test",
            LastName = "User",
            Gender = "X",
            Email = "test@example.com",
            PasswordHash = "hash",
            PhoneNumber = "",
            UserName = "test",
            CompanyId = 7,
            AllowedModulesJson = "[\"sales\",\"customers\"]"
        };

        var result = Assert.IsType<OkObjectResult>(await controller.Modules(
            new StubEntitlements(new[] { "sales", "customers", "billing" }),
            new StubUsers(currentUser)));
        var payload = JsonSerializer.SerializeToElement(result.Value);

        Assert.Equal(new[] { "sales", "customers" }, Strings(payload, "assignedModules"));
        Assert.Equal(new[] { "sales", "customers" }, Strings(payload, "modules"));
    }

    private static async Task<JsonElement> GetModules(string assignment, IReadOnlyCollection<string> licensedModules)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", "42"),
            new Claim("companyId", "7"),
        }, "Test");
        var controller = new AuthController(null!)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };

        var result = await controller.Modules(
            new StubEntitlements(licensedModules),
            new StubUsers(new User
            {
                Id = 42,
                FirstName = "Test",
                LastName = "User",
                Gender = "X",
                Email = "test@example.com",
                PasswordHash = "hash",
                PhoneNumber = "",
                UserName = "test",
                CompanyId = 7,
                AllowedModulesJson = assignment
            }));
        var objectResult = Assert.IsType<OkObjectResult>(result);
        return JsonSerializer.SerializeToElement(objectResult.Value);
    }

    private static string[] Strings(JsonElement response, string property) => response
        .GetProperty(property)
        .EnumerateArray()
        .Select(item => item.GetString()!)
        .ToArray();

    private sealed class StubEntitlements(IReadOnlyCollection<string> modules) : ICompanyEntitlementService
    {
        public Task<CompanyEntitlements> GetAsync(int companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CompanyEntitlements(modules.ToHashSet(StringComparer.Ordinal), new HashSet<string>()));

        public void Invalidate(int companyId) { }
    }

    private sealed class StubUsers(User user) : IUserService
    {
        public Task<User?> GetByIdAsync(int id) => Task.FromResult<User?>(id == user.Id ? user : null);
        public Task<IEnumerable<User>> GetAllAsync() => throw new NotImplementedException();
        public Task<User> CreateAsync(User value) => throw new NotImplementedException();
        public Task AssignCompanyAsync(int userId, int companyId) => throw new NotImplementedException();
        public Task SetRoleAsync(int userId, string role) => throw new NotImplementedException();
        public Task SetActiveAsync(int userId, bool active) => throw new NotImplementedException();
        public Task UpdateAsync(User value) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
        public Task<IEnumerable<User>> GetCashiersAsync() => throw new NotImplementedException();
    }

}
