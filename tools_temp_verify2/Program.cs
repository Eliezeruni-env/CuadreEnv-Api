using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Onion.DataAccess;
using Onion.Domain.Users;
using Onion.BussinesLogic.Services.Concrete;
using Onion.BussinesLogic.Dtos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Microsoft.Extensions.Configuration;

class StaticTenantProvider : ITenantProvider
{
    public int? GetCompanyId() => null;
}

class Program
{
    static async Task<int> Main(string[] args)
    {
        var connStr = "Host=aws-0-us-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.uhgmavivdoggwanimhqa;Password=!pk%kPbm+gU27_,;Ssl Mode=Require;Timeout=60;Command Timeout=60;Keepalive=30;";
        var options = new DbContextOptionsBuilder<OnionDbContext>()
            .UseNpgsql(connStr)
            .Options;

        var ctx = new OnionDbContext(options, new StaticTenantProvider());
        var uow = new Onion.DataAccess.Repositories.Concrete.UnitOfWork(ctx, null);

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            {"Jwt:Key", "dev-local-key-please-change-in-production-32chars!"},
            {"Jwt:Issuer", "onion-api"},
            {"Jwt:Audience", "onion-api-audience"}
        }).Build();

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Production");

        var httpCtxAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor();

        var authService = new AuthService(
            uow,
            config,
            new NullLogger<AuthService>(),
            envMock.Object,
            httpCtxAccessor,
            null
        );

        try
        {
            var res = await authService.LoginAsync(new LoginRequestDto("moises1@cuadre.com", "RXwwfW3ccn"));
            Console.WriteLine("LOGIN SUCCESS!");
            Console.WriteLine($"AccessToken: {res.AccessToken}");
            Console.WriteLine($"User: {res.User.Email}, CompanyId: {res.User.CompanyId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LOGIN FAILED: {ex.Message}");
            if (ex is Onion.Common.Exceptions.CustomException cex)
            {
                foreach (var err in cex.Errors) Console.WriteLine($"Error: {err.Code} - {err.Message}");
            }
        }

        return 0;
    }
}
