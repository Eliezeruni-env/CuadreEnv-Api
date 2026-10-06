using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Onion.Common.Services;
using Onion.DataAccess;
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

    private static OnionDbContext CreateContext(string databaseName, int? companyId, bool isGlobalAdministrator = false)
    {
        var options = new DbContextOptionsBuilder<OnionDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new OnionDbContext(
            options,
            currentUserService: new TestCurrentUserService(companyId, isGlobalAdministrator));
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
