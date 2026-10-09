using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Onion.Controllers;
using Onion.DataAccess;
using Onion.Domain.Users;
using Xunit;

namespace Onion.Tests;

public class InternalUserControllerTests
{
    [Fact]
    public async Task ResolveUser_PrefersExternalId_WhenExternalIdIsNumeric()
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());
        db.Users.AddRange(CreateUser(10, 10, "12345"), CreateUser(12345, 20, "other-external-id"));
        await db.SaveChangesAsync();

        var result = await ResolveAsync(db, "12345");

        Assert.NotNull(result);
        Assert.Equal(10, result!.Id);
    }

    [Fact]
    public async Task ResolveUser_FindsLocalIdAcrossTenantQueryFilter()
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());
        db.Users.Add(CreateUser(27, 20, "external-27"));
        await db.SaveChangesAsync();

        var result = await ResolveAsync(db, "27");

        Assert.NotNull(result);
        Assert.Equal(27, result!.Id);
    }

    private static async Task<User?> ResolveAsync(OnionDbContext db, string identifier)
    {
        var controller = new InternalUserController(null!, db, null!, null!);
        var method = typeof(InternalUserController).GetMethod(
            "FindUserByIdOrExternalIdAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        var task = (Task<User?>)method!.Invoke(controller, new object[] { identifier })!;
        return await task;
    }

    private static OnionDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<OnionDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new OnionDbContext(options, currentUserService: new TenantUserService(10));
    }

    private static User CreateUser(int id, int companyId, string identification) => new()
    {
        Id = id,
        CompanyId = companyId,
        Identification = identification,
        Email = $"user-{id}@example.test",
        UserName = $"user-{id}",
        FirstName = "Test",
        LastName = "User",
        Gender = "M",
        PasswordHash = "hash",
        PhoneNumber = string.Empty,
        CreationDate = DateTime.UtcNow
    };

    private sealed class TenantUserService(int companyId) : Onion.Common.Services.ICurrentUserService
    {
        public int? CompanyId { get; } = companyId;
        public int? UserId => 1;
        public string? ClerkUserId => "1";
        public string? UserEmail => "test@example.test";
        public string? UserRole => "Admin";
        public string? IpAddress => null;
        public string? UserAgent => null;
        public bool IsAuthenticated => true;
        public bool IsGlobalAdministrator => false;
    }
}
