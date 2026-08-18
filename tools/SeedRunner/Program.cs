using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

class Program
{
    static int Main(string[] args)
    {
        try
        {
            // Allow connection string via environment variable first
            var envConn = Environment.GetEnvironmentVariable("ConnectionStrings__OnionCrud");
            string? conn = null;
            if (!string.IsNullOrWhiteSpace(envConn))
            {
                conn = envConn;
            }
            else
            {
                // Try to find appsettings.json by walking up directories (project root detection)
                string? repoRoot = null;
                var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
                for (int i = 0; i < 8 && dir != null; i++)
                {
                    var candidate = Path.Combine(dir.FullName, "crud-onion", "appsettings.json");
                    if (File.Exists(candidate))
                    {
                        repoRoot = dir.FullName;
                        break;
                    }
                    dir = dir.Parent;
                }

                IConfigurationRoot? config = null;
                if (repoRoot != null)
                {
                    config = new ConfigurationBuilder()
                        .SetBasePath(Path.Combine(repoRoot, "crud-onion"))
                        .AddJsonFile("appsettings.json", optional: true)
                        .AddEnvironmentVariables()
                        .Build();
                    conn = config.GetConnectionString("OnionCrud");
                }
            }
            if (string.IsNullOrWhiteSpace(conn))
            {
                Console.WriteLine("Connection string 'OnionCrud' not found in appsettings.json or environment variables. Exiting.");
                return 2;
            }

            // Find seed script by walking up from current directory
            string? seedPath = null;
            var cur = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (int i = 0; i < 8 && cur != null; i++)
            {
                var candidate = Path.Combine(cur.FullName, "DataAccess", "Seeds", "seed_sample_data.sql");
                if (File.Exists(candidate))
                {
                    seedPath = candidate;
                    break;
                }
                cur = cur.Parent;
            }

            if (seedPath == null)
            {
                Console.WriteLine("Seed script not found under repository (DataAccess/Seeds/seed_sample_data.sql)");
                return 3;
            }

            var script = File.ReadAllText(seedPath);
            using var connSql = new SqlConnection(conn);
            connSql.Open();
            using var cmd = connSql.CreateCommand();
            cmd.CommandText = script;
            cmd.CommandTimeout = 120;
            cmd.ExecuteNonQuery();
            Console.WriteLine("Seed script executed successfully.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error executing seed: " + ex.Message);
            return 1;
        }
    }
}
