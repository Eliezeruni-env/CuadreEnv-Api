using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Onion.BussinesLogic.Services.Concrete;
using Onion.DataAccess;
using Xunit;

namespace Onion.Tests;

public sealed class AccountStatusServiceTests
{
    [Fact]
    public async Task Returns_False_For_Inactive_Or_Deleted_User()
    {
        var options = new DbContextOptionsBuilder<OnionDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new OnionDbContext(options);
        db.Users.Add(new Onion.Domain.Users.User
        {
            Id = 1, Email = "inactive@test.local", UserName = "inactive", PasswordHash = "x",
            FirstName = "Inactive", LastName = "User", Gender = "M", PhoneNumber = "", Active = false, IsDeleted = false
        });
        db.Users.Add(new Onion.Domain.Users.User
        {
            Id = 2, Email = "deleted@test.local", UserName = "deleted", PasswordHash = "x",
            FirstName = "Deleted", LastName = "User", Gender = "M", PhoneNumber = "", Active = true, IsDeleted = true
        });
        await db.SaveChangesAsync();
        var service = new AccountStatusService(db);

        Assert.False(await service.IsActiveAsync(1));
        Assert.False(await service.IsActiveAsync(2));
    }
}
