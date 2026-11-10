using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Onion.BussinesLogic.Dtos;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;
using FluentAssertions;

namespace Onion.IntegrationTests
{
    public class AuthAndWarehouseFlow : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly string _databaseName;

        public AuthAndWarehouseFlow(WebApplicationFactory<Program> factory)
        {
            _databaseName = "OnionTest_" + Guid.NewGuid().ToString("N");
            var baseConnection = Environment.GetEnvironmentVariable("ConnectionStrings__OnionCrud");
            if (string.IsNullOrWhiteSpace(baseConnection))
                throw new InvalidOperationException("Integration tests require ConnectionStrings__OnionCrud pointing to a PostgreSQL server.");

            var connectionBuilder = new NpgsqlConnectionStringBuilder(baseConnection)
            {
                Database = _databaseName
            };
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("IntegrationTesting");
                builder.ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:OnionCrud"] = connectionBuilder.ConnectionString,
                        ["ApplyMigrationsOnStartup"] = "true",
                        ["Usm:ValidateOnStartup"] = "false",
                        ["Usm:BaseUrl"] = "http://localhost:3000",
                        ["Authorization:BypassRoleAndModuleChecks"] = "false",
                        ["Jwt:Key"] = "integration-only-test-key-32-characters-minimum!",
                        ["Jwt:Issuer"] = "onion-api",
                        ["Jwt:Audience"] = "onion-api-audience",
                        ["Cors:AllowedOrigins"] = "http://localhost"
                    });
                });

                // Test-only: register a custom output formatter that serializes to a pre-built string
                // and writes via Response.WriteAsync to avoid TestServer PipeWriter UnflushedBytes issues.
                // This formatter is registered only inside the test project's WebApplicationFactory configuration
                // and does not touch production Startup/Program.cs.
                builder.ConfigureServices(services =>
                {
                    services.PostConfigure<MvcOptions>(opts =>
                    {
                        // Insert at the front so it takes precedence over SystemTextJsonOutputFormatter
                        opts.OutputFormatters.Insert(0, new TestStringJsonOutputFormatter());
                    });

                    // Replace IEmailService with test stub
                    services.AddSingleton<Onion.Common.Services.IEmailService, TestEmailService>();
                });
            });
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await _factory.DisposeAsync();

            var baseConnection = Environment.GetEnvironmentVariable("ConnectionStrings__OnionCrud");
            if (string.IsNullOrWhiteSpace(baseConnection)) return;
            var connectionBuilder = new NpgsqlConnectionStringBuilder(baseConnection)
            {
                Database = "postgres"
            };

            await using var connection = new NpgsqlConnection(connectionBuilder.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @databaseName AND pid <> pg_backend_pid(); DROP DATABASE IF EXISTS \"" + _databaseName + "\";";
            command.Parameters.AddWithValue("databaseName", _databaseName);
            await command.ExecuteNonQueryAsync();
        }

        [Fact]
        public async Task RegisterLoginDecodeJwtAndCallWarehouse_ShouldWork()
        {
            var client = _factory.CreateClient();

            // Register a new company + user (CreateCompanyName ensures company created)
            var register = new
            {
                Email = $"itest+{Guid.NewGuid().ToString("N").Substring(0,8)}@example.com",
                Password = "P@ssw0rd!",
                FirstName = "Integration",
                LastName = "Tester",
                CreateCompanyName = "IntegrationTestCo"
            };

            var regResp = await client.PostAsync("/v1/auth/register", new StringContent(JsonSerializer.Serialize(register), Encoding.UTF8, "application/json"));
            var registrationBody = await regResp.Content.ReadAsStringAsync();
            regResp.IsSuccessStatusCode.Should().BeTrue($"registration failed with {(int)regResp.StatusCode}: {registrationBody}");

            // Login
            var login = new { Email = register.Email, Password = register.Password };
            var loginResp = await client.PostAsync("/v1/auth/login", new StringContent(JsonSerializer.Serialize(login), Encoding.UTF8, "application/json"));
            var loginFailureBody = await loginResp.Content.ReadAsStringAsync();
            loginResp.IsSuccessStatusCode.Should().BeTrue($"login failed with {(int)loginResp.StatusCode}: {loginFailureBody}");
            var loginBody = await loginResp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(loginBody);
            var access = doc.RootElement.TryGetProperty("accessToken", out var at) ? at.GetString() : doc.RootElement.GetProperty("AccessToken").GetString();
            access.Should().NotBeNullOrEmpty();

            // Second login attempt with same credentials should also succeed and return a token
            var loginResp2 = await client.PostAsync("/v1/auth/login", new StringContent(JsonSerializer.Serialize(login), Encoding.UTF8, "application/json"));
            loginResp2.EnsureSuccessStatusCode();
            var loginBody2 = await loginResp2.Content.ReadAsStringAsync();
            using var doc2 = JsonDocument.Parse(loginBody2);
            var access2 = doc2.RootElement.TryGetProperty("accessToken", out var at2) ? at2.GetString() : doc2.RootElement.GetProperty("AccessToken").GetString();
            access2.Should().NotBeNullOrEmpty();

            // Decode payload and assert CompanyId and role present
            var parts = access!.Split('.');
            parts.Length.Should().BeGreaterThan(2);
            var payload = parts[1];
            var mod4 = payload.Length % 4;
            if (mod4 > 0) payload += new string('=', 4 - mod4);
            var bytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
            var jsonPayload = Encoding.UTF8.GetString(bytes);
            var payloadDoc = JsonDocument.Parse(jsonPayload);
            payloadDoc.RootElement.TryGetProperty("CompanyId", out var companyIdProp).Should().BeTrue();
            companyIdProp.GetString().Should().NotBeNullOrEmpty();
            payloadDoc.RootElement.TryGetProperty("role", out var roleProp).Should().BeTrue();
            roleProp.GetString().Should().NotBeNullOrEmpty();

            // A newly registered tenant starts without operational module assignments.
            // Verify the authenticated tenant boundary without granting permissions implicitly.
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
            var whResp = await client.GetAsync("/v1/warehouse");
            whResp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            client.DefaultRequestHeaders.Authorization = null;
            var anonymousResponse = await client.GetAsync("/v1/warehouse");
            anonymousResponse.StatusCode.Should().BeOneOf(
                System.Net.HttpStatusCode.Unauthorized,
                System.Net.HttpStatusCode.Forbidden);

        }

        [Fact]
        public async Task ConcurrentInventoryReductions_ShouldAllowOnlyOneReductionOfLastUnit()
        {
            var companyId = Random.Shared.Next(100000, 900000);
            var productId = Random.Shared.Next(100000, 900000);
            var warehouseId = Random.Shared.Next(100000, 900000);

            await using (var setup = await OpenTestConnectionAsync())
            await using (var command = setup.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO ""Inventories"" (""ProductId"", ""WarehouseId"", ""Quantity"", ""CompanyId"", ""CreationDate"", ""Active"", ""IsDeleted"", ""CreateBy"", ""ModifiedBy"")
                    VALUES (@productId, @warehouseId, 1, @companyId, now(), true, false, 'integration-test', 'integration-test')";
                command.Parameters.AddWithValue("productId", productId);
                command.Parameters.AddWithValue("warehouseId", warehouseId);
                command.Parameters.AddWithValue("companyId", companyId);
                await command.ExecuteNonQueryAsync();
            }

            async Task<bool> ReduceAsync()
            {
                await using var connection = await OpenTestConnectionAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE ""Inventories""
                    SET ""Quantity"" = ""Quantity"" - 1
                    WHERE ""ProductId"" = @productId AND ""WarehouseId"" = @warehouseId
                      AND ""CompanyId"" = @companyId AND ""Quantity"" >= 1";
                command.Parameters.AddWithValue("productId", productId);
                command.Parameters.AddWithValue("warehouseId", warehouseId);
                command.Parameters.AddWithValue("companyId", companyId);
                return await command.ExecuteNonQueryAsync() == 1;
            }

            var results = await Task.WhenAll(ReduceAsync(), ReduceAsync());

            results.Count(result => result).Should().Be(1);
            results.Count(result => !result).Should().Be(1);

            await using var verify = await OpenTestConnectionAsync();
            await using var verifyCommand = verify.CreateCommand();
            verifyCommand.CommandText = "SELECT \"Quantity\" FROM \"Inventories\" WHERE \"ProductId\" = @productId AND \"WarehouseId\" = @warehouseId AND \"CompanyId\" = @companyId";
            verifyCommand.Parameters.AddWithValue("productId", productId);
            verifyCommand.Parameters.AddWithValue("warehouseId", warehouseId);
            verifyCommand.Parameters.AddWithValue("companyId", companyId);
            (await verifyCommand.ExecuteScalarAsync()).Should().BeOfType<decimal>().Which.Should().Be(0m);
        }

        [Fact]
        public async Task ConcurrentCashRegisterClosures_ShouldPersistOnlyOneClosure()
        {
            var companyId = Random.Shared.Next(100000, 900000);

            await using (var setup = await OpenTestConnectionAsync())
            await using (var command = setup.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO ""CashRegisters"" (""OpeningAmount"", ""InitialAmount"", ""OpenedAt"", ""CompanyId"", ""IsOpen"", ""Status"", ""IsImmutable"", ""RowVersion"", ""CreationDate"", ""Active"", ""IsDeleted"", ""CreateBy"", ""ModifiedBy"")
                    VALUES (100, 100, now(), @companyId, true, 0, false, decode(repeat('00', 8), 'hex'), now(), true, false, 'integration-test', 'integration-test')
                    RETURNING ""Id""";
                command.Parameters.AddWithValue("companyId", companyId);
                var registerId = (int)(await command.ExecuteScalarAsync())!;

                async Task<bool> CloseAsync()
                {
                    await using var connection = await OpenTestConnectionAsync();
                    await using var close = connection.CreateCommand();
                    close.CommandText = @"
                        UPDATE ""CashRegisters""
                        SET ""ClosingAmount"" = 100, ""ClosedAt"" = now(), ""IsOpen"" = false, ""Status"" = 2, ""IsImmutable"" = true
                        WHERE ""Id"" = @id AND ""Status"" <> 2 AND ""IsImmutable"" = false";
                    close.Parameters.AddWithValue("id", registerId);
                    return await close.ExecuteNonQueryAsync() == 1;
                }

                var results = await Task.WhenAll(CloseAsync(), CloseAsync());
                results.Count(result => result).Should().Be(1);
                results.Count(result => !result).Should().Be(1);
            }
        }

        [Fact]
        public async Task ConcurrentDuplicateIdempotencyRequests_ShouldPersistOnlyOneRequest()
        {
            var companyId = Random.Shared.Next(100000, 900000);
            var key = "itest-" + Guid.NewGuid().ToString("N");
            const string requestPath = "/v1/integration/idempotency";

            async Task<bool> InsertAsync()
            {
                try
                {
                    await using var connection = await OpenTestConnectionAsync();
                    await using var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO ""ProcessedRequests"" (""CompanyId"", ""IdempotencyKey"", ""RequestPath"", ""StatusCode"", ""ResponseJson"", ""CreatedAtUtc"", ""ExpiresAtUtc"")
                        VALUES (@companyId, @key, @path, 200, '{}', now(), now() + interval '1 hour')";
                    command.Parameters.AddWithValue("companyId", companyId);
                    command.Parameters.AddWithValue("key", key);
                    command.Parameters.AddWithValue("path", requestPath);
                    await command.ExecuteNonQueryAsync();
                    return true;
                }
                catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    return false;
                }
            }

            var results = await Task.WhenAll(InsertAsync(), InsertAsync());

            results.Count(result => result).Should().Be(1);
            results.Count(result => !result).Should().Be(1);
        }

        private async Task<NpgsqlConnection> OpenTestConnectionAsync()
        {
            var baseConnection = Environment.GetEnvironmentVariable("ConnectionStrings__OnionCrud");
            baseConnection.Should().NotBeNullOrWhiteSpace();
            var builder = new NpgsqlConnectionStringBuilder(baseConnection)
            {
                Database = _databaseName
            };
            var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        // Test-only output formatter used to avoid TestServer PipeWriter issues.
        // This class lives only inside the integration test project and is registered
        // exclusively in the WebApplicationFactory configuration above.
        private class TestStringJsonOutputFormatter : TextOutputFormatter
        {
            private readonly JsonSerializerOptions _options;

            public TestStringJsonOutputFormatter()
            {
                SupportedMediaTypes.Add("application/json");
                SupportedMediaTypes.Add("text/json");
                SupportedEncodings.Add(System.Text.Encoding.UTF8);
                SupportedEncodings.Add(System.Text.Encoding.Unicode);

                _options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                };
            }

            protected override bool CanWriteType(Type? type)
            {
                return true; // attempt to handle any type
            }

            public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, System.Text.Encoding selectedEncoding)
            {
                var response = context.HttpContext.Response;
                var json = System.Text.Json.JsonSerializer.Serialize(context.Object, _options);
                response.ContentType ??= "application/json; charset=utf-8";
                await response.WriteAsync(json);
            }
        }
    }
}
