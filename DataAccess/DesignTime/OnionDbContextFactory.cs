using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;

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

            string? conn = null;
            var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            if (File.Exists(settingsPath))
            {
                try
                {
                    using var fs = File.OpenRead(settingsPath);
                    using var doc = JsonDocument.Parse(fs);
                    if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                        cs.TryGetProperty("OnionCrud", out var onionCrud))
                    {
                        conn = onionCrud.GetString();
                    }
                }
                catch
                {
                    // ignore and fallback
                }
            }

            if (string.IsNullOrWhiteSpace(conn))
            {
                // fallback to localdb
                conn = "Server=(localdb)\\MSSQLLocalDB;Database=onionCrud;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";
            }

            var builder = new DbContextOptionsBuilder<OnionDbContext>();
            builder.UseSqlServer(conn, b => b.MigrationsAssembly("Onion.DataAccess"));

            return new OnionDbContext(builder.Options);
        }
    }
}
