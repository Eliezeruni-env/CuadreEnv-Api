using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Onion.Common.Services;
using Onion.Controllers;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain.Users;
using Xunit;

namespace Onion.Tests;

public sealed class AuthControllerModuleTests
{
    [Fact]
    public async Task Modules_IntersectsWildcardAssignmentWithCompanyEntitlements()
    {
        var controller = CreateController(
            new Claim("companyId", "7"),
            new Claim("allowedModules", "[\"*\"]"));
        var entitlements = new StubEntitlements("audit", "company");

        var response = Assert.IsType<OkObjectResult>(await controller.Modules(
            entitlements,
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
                AllowedModulesJson = "[\"*\"]"
            })));
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(response.Value));
        var modules = payload.RootElement.GetProperty("modules")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();
        var licensedModules = payload.RootElement.GetProperty("licensedModules")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Equal(new[] { "audit", "company" }, licensedModules);
        Assert.Equal(new[] { "audit", "company" }, modules);
        Assert.False(payload.RootElement.GetProperty("hasAllModules").GetBoolean());
    }

    [Fact]
    public async Task Modules_IntersectsExplicitUserAssignmentWithCompanyEntitlements()
    {
        var controller = CreateController(
            new Claim("companyId", "7"),
            new Claim("allowedModules", "[\"audit\",\"company\"]"));
        var entitlements = new StubEntitlements("audit");

        var response = Assert.IsType<OkObjectResult>(await controller.Modules(
            entitlements,
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
                AllowedModulesJson = "[\"audit\",\"company\"]"
            })));
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(response.Value));
        var modules = payload.RootElement.GetProperty("modules")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Equal(new[] { "audit" }, modules);
    }

    private static AuthController CreateController(params Claim[] claims)
    {
        var controller = new AuthController(null!);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim("sub", "42") }.Concat(claims), "test"))
            }
        };
        return controller;
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

    private sealed class StubEntitlements(params string[] modules) : ICompanyEntitlementService
    {
        public Task<CompanyEntitlements> GetAsync(int companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CompanyEntitlements(
                modules.ToHashSet(StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal)));

        public void Invalidate(int companyId) { }
    }
}
