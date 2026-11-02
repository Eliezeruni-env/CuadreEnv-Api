using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Onion.DataAccess.DesignTime
{
    // Design-time factory so EF tools can create OnionDbContext without running the app Program
    public class OnionDbContextFactory : IDesignTimeDbContextFactory<OnionDbContext>
    {
        public OnionDbContext CreateDbContext(string[] args)
        {
            // Try to locate appsettings.json starting from the current directory and walking up
            var basePath = Directory.GetCurrentDirectory();
            for (int i = 0; i < 5; i++)
            {
                var candidate = Path.Combine(basePath, "appsettings.json");
                if (File.Exists(candidate))
                {
                    break;
                }
                basePath = Path.GetDirectoryName(basePath) ?? basePath;
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
            var conn = configuration.GetConnectionString("OnionCrud");
            if (string.IsNullOrWhiteSpace(conn))
            {
                throw new InvalidOperationException(
                    "EF design-time operations require ConnectionStrings:OnionCrud or ConnectionStrings__OnionCrud to be configured; no default database credentials are provided.");
            }

            var builder = new DbContextOptionsBuilder<OnionDbContext>();
            builder.UseNpgsql(conn, b => b.MigrationsAssembly("Onion.DataAccess"));

            return new OnionDbContext(builder.Options);
        }
    }
}
