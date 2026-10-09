using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Concrete;
using Onion.Common.Services;
using Onion.DataAccess;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain.Users;
using Xunit;

namespace Onion.Tests;

public sealed class UserTenantIsolationTests
{
    [Fact]
    public async Task Tenant_A_Cannot_Read_Tenant_B_Users_Or_SoftDeleted_Users()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var database = CreateContext(databaseName, 10);
        database.Users.AddRange(
            CreateUser(1, 10, "a@example.test"),
            CreateUser(3, 10, "deleted@example.test", isDeleted: true));
        await database.SaveChangesAsync();

        await using var tenantB = CreateContext(databaseName, 20);
        tenantB.Users.Add(CreateUser(2, 20, "b@example.test"));
        await tenantB.SaveChangesAsync();

        await using var tenantA = CreateContext(databaseName, 10);
        var visible = await tenantA.Users.Select(u => u.Id).ToListAsync();

        Assert.Equal(new[] { 1 }, visible);
    }

    [Fact]
    public async Task Global_Administrator_Can_Read_All_NonDeleted_Users()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var database = CreateContext(databaseName, 10);
        database.Users.Add(CreateUser(1, 10, "a@example.test"));
        await database.SaveChangesAsync();

        await using var tenantB = CreateContext(databaseName, 20);
        tenantB.Users.Add(CreateUser(2, 20, "b@example.test"));
        await tenantB.SaveChangesAsync();

        await using var global = CreateContext(databaseName, null, isGlobalAdministrator: true);
        var visible = await global.Users.Select(u => u.Id).OrderBy(id => id).ToListAsync();

        Assert.Equal(new[] { 1, 2 }, visible);
    }

    [Fact]
    public async Task Tenant_Admin_Cannot_Read_Users_From_Other_Tenants()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var database = CreateContext(databaseName, 10);
        database.Users.Add(CreateUser(1, 10, "a-admin@example.test"));
        await database.SaveChangesAsync();

        await using var tenantB = CreateContext(databaseName, 20);
        tenantB.Users.Add(CreateUser(2, 20, "b-employee@example.test"));
        await tenantB.SaveChangesAsync();

        await using var tenantAdmin = CreateContext(databaseName, 10, isGlobalAdministrator: false);
        var visible = await tenantAdmin.Users.Select(u => u.Id).ToListAsync();

        Assert.Equal(new[] { 1 }, visible);
        Assert.False(tenantAdmin.IsGlobalTenantAccess);
    }

    [Fact]
    public async Task Tenant_Admin_UserManagementService_GetPaged_Forces_Caller_Tenant_And_Excludes_Other_Tenants()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var ctx10 = CreateContext(databaseName, 10))
        {
            ctx10.Users.AddRange(
                CreateUser(1, 10, "admin-t10@example.test"),
                CreateUser(2, 10, "emp-t10@example.test")
            );
            await ctx10.SaveChangesAsync();
        }

        await using (var ctx20 = CreateContext(databaseName, 20))
        {
            ctx20.Users.Add(CreateUser(3, 20, "emp-t20@example.test"));
            await ctx20.SaveChangesAsync();
        }

        var tenant10User = new TestCurrentUserService(companyId: 10, isGlobalAdministrator: false);
        await using var ctx = CreateContext(databaseName, currentUserService: tenant10User);
        var service = CreateUserManagementService(ctx, tenant10User);

        // Attempt to pass companyId 20 as query filter - service must force it to tenant 10
        var result = await service.GetPagedAsync(pageNumber: 1, pageSize: 10, q: null, role: null, active: null, companyId: 20);

        Assert.Equal(2, result.Total);
        Assert.All(result.Items, u => Assert.Equal(10, u.CompanyId));
        Assert.DoesNotContain(result.Items, u => u.Id == 3);
    }

    [Fact]
    public async Task Tenant_Admin_UserManagementService_GetById_Returns_Null_For_Other_Tenant_User()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var ctx10 = CreateContext(databaseName, 10))
        {
            ctx10.Users.Add(CreateUser(1, 10, "emp-t10@example.test"));
            await ctx10.SaveChangesAsync();
        }

        await using (var ctx20 = CreateContext(databaseName, 20))
        {
            ctx20.Users.Add(CreateUser(2, 20, "emp-t20@example.test"));
            await ctx20.SaveChangesAsync();
        }

        var tenant10User = new TestCurrentUserService(companyId: 10, isGlobalAdministrator: false);
        await using var ctx = CreateContext(databaseName, currentUserService: tenant10User);
        var service = CreateUserManagementService(ctx, tenant10User);

        var ownUser = await service.GetByIdAsync(1);
        Assert.NotNull(ownUser);
        Assert.Equal(10, ownUser.CompanyId);

        var otherUser = await service.GetByIdAsync(2);
        Assert.Null(otherUser);
    }

    [Fact]
    public async Task Tenant_Admin_UserManagementService_Cannot_Update_Or_Delete_Other_Tenant_User()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var ctx10 = CreateContext(databaseName, 10))
        {
            ctx10.Users.Add(CreateUser(1, 10, "emp-t10@example.test"));
            await ctx10.SaveChangesAsync();
        }

        await using (var ctx20 = CreateContext(databaseName, 20))
        {
            ctx20.Users.Add(CreateUser(2, 20, "emp-t20@example.test"));
            await ctx20.SaveChangesAsync();
        }

        var tenant10User = new TestCurrentUserService(companyId: 10, isGlobalAdministrator: false);
        await using var ctx = CreateContext(databaseName, currentUserService: tenant10User);
        var service = CreateUserManagementService(ctx, tenant10User);

        var updateReq = new UpdateUserRequest
        {
            FirstName = "Hacked",
            LastName = "User",
            Email = "emp-t20@example.test",
            UserName = "emp-t20@example.test",
            Role = "Employee"
        };

        var updateEx = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(2, updateReq, "performedBy10"));
        Assert.Equal("User not found", updateEx.Message);

        var deleteEx = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync(2, "performedBy10"));
        Assert.Equal("User not found", deleteEx.Message);
    }

    [Fact]
    public async Task UserManagementService_Create_Persists_Explicit_Allowed_Modules()
    {
        await using var ctx = CreateContext(Guid.NewGuid().ToString(), companyId: 10);
        ctx.Companies.Add(new Onion.Domain.Company { Id = 10, Name = "Tenant" });
        await ctx.SaveChangesAsync();
        var currentUser = new TestCurrentUserService(companyId: 10, isGlobalAdministrator: false);
        var service = CreateUserManagementService(ctx, currentUser);

        var created = await service.CreateAsync(new CreateUserRequest
        {
            FirstName = "Test",
            LastName = "Employee",
            Email = "new-employee@example.test",
            Role = "Employee",
            TemporaryPassword = "SafeTemporaryPassword1!",
            AllowedModules = new() { "sales", "cash-register", "customers" }
        }, "admin");

        var stored = await ctx.Users.SingleAsync(user => user.Id == created.Id);
        Assert.Equal(new[] { "sales", "cashregister", "customers" }, JsonModuleList(stored.AllowedModulesJson));
        Assert.Equal(new[] { "sales", "cashregister", "customers" }, created.AllowedModules);
        var listed = await service.GetPagedAsync(1, 10, null, null, null, null);
        Assert.Equal(new[] { "sales", "cashregister", "customers" }, listed.Items.Single().AllowedModules);
    }

    [Fact]
    public async Task UserManagementService_Update_Persists_Module_Changes_And_Leaves_Omitted_Assignments_Alone()
    {
        await using var ctx = CreateContext(Guid.NewGuid().ToString(), companyId: 10);
        var user = CreateUser(1, 10, "employee@example.test");
        user.AllowedModulesJson = "[\"sales\"]";
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        var currentUser = new TestCurrentUserService(companyId: 10, isGlobalAdministrator: false);
        var service = CreateUserManagementService(ctx, currentUser);

        await service.UpdateAsync(1, new UpdateUserRequest
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            UserName = user.UserName,
            Role = user.Role,
            AllowedModules = new() { "cash-register", "customers" }
        }, "admin");

        var updated = await service.GetByIdAsync(1);
        Assert.Equal(new[] { "cashregister", "customers" }, updated?.AllowedModules);

        await service.UpdateAsync(1, new UpdateUserRequest
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            UserName = user.UserName,
            Role = user.Role
        }, "admin");

        var unchanged = await service.GetByIdAsync(1);
        Assert.Equal(new[] { "cashregister", "customers" }, unchanged?.AllowedModules);
    }

    private static string[] JsonModuleList(string? json) =>
        System.Text.Json.JsonSerializer.Deserialize<string[]>(json ?? "[]") ?? Array.Empty<string>();

    private static UserManagementService CreateUserManagementService(OnionDbContext context, ICurrentUserService currentUserService)
    {
        var repo = new GenericRepository<User>(context);
        var logger = NullLogger<UserManagementService>.Instance;
        var config = new ConfigurationBuilder().Build();
        var httpContextAccessor = new HttpContextAccessor();
        return new UserManagementService(repo, context, currentUserService, logger, emailService: null, config: config, httpContextAccessor: httpContextAccessor);
    }

    private static OnionDbContext CreateContext(string databaseName, int? companyId, bool isGlobalAdministrator = false)
    {
        return CreateContext(databaseName, new TestCurrentUserService(companyId, isGlobalAdministrator));
    }

    private static OnionDbContext CreateContext(string databaseName, ICurrentUserService currentUserService)
    {
        var options = new DbContextOptionsBuilder<OnionDbContext>()
            .UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(warnings => warnings.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new OnionDbContext(
            options,
            currentUserService: currentUserService);
    }

    private static User CreateUser(int id, int companyId, string email, bool isDeleted = false) => new()
    {
        Id = id,
        CompanyId = companyId,
        Email = email,
        UserName = email,
        FirstName = "Test",
        LastName = "User",
        Gender = "M",
        PasswordHash = "hash",
        PhoneNumber = string.Empty,
        IsDeleted = isDeleted,
        CreationDate = DateTime.UtcNow
    };

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public TestCurrentUserService(int? companyId = null, bool isGlobalAdministrator = false)
        {
            CompanyId = companyId;
            IsGlobalAdministrator = isGlobalAdministrator;
        }

        public int? CompanyId { get; }
        public int? UserId => 999;
        public string? ClerkUserId => UserId.ToString();
        public string? UserEmail => "test@example.test";
        public string? UserRole => "Admin";
        public string? IpAddress => null;
        public string? UserAgent => null;
        public bool IsAuthenticated => CompanyId.HasValue || IsGlobalAdministrator;
        public bool IsGlobalAdministrator { get; }
    }
}
