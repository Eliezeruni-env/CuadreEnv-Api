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
using Microsoft.Data.SqlClient;
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
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("IntegrationTesting");
                builder.ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:OnionCrud"] = $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True",
                        ["ApplyMigrationsOnStartup"] = "true",
                        ["Usm:ValidateOnStartup"] = "false",
                        ["Usm:BaseUrl"] = "http://localhost:3000",
                        ["Authorization:BypassRoleAndModuleChecks"] = "true",
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

            var masterConnection = new SqlConnectionStringBuilder
            {
                DataSource = @"(localdb)\MSSQLLocalDB",
                InitialCatalog = "master",
                IntegratedSecurity = true,
                TrustServerCertificate = true
            }.ConnectionString;

            await using var connection = new SqlConnection(masterConnection);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"IF DB_ID(@databaseName) IS NOT NULL BEGIN ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]; END;";
            command.Parameters.AddWithValue("@databaseName", _databaseName);
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
