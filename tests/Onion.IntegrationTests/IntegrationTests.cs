using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Onion.BussinesLogic.Dtos;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;
using FluentAssertions;

namespace Onion.IntegrationTests
{
    public class AuthAndWarehouseFlow : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public AuthAndWarehouseFlow(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                // Use a unique LocalDB database per test run by overriding configuration
                var dbName = "OnionTest_" + Guid.NewGuid().ToString("N");
                builder.ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = $"Server=(localdb)\\MSSQLLocalDB;Database={dbName};Trusted_Connection=True;MultipleActiveResultSets=true"
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
                });
            });
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
                CreateCompanyName = "IntegrationTestCo"
            };

            var regResp = await client.PostAsync("/auth/register", new StringContent(JsonSerializer.Serialize(register), Encoding.UTF8, "application/json"));
            regResp.EnsureSuccessStatusCode();

            // Login
            var login = new { Email = register.Email, Password = register.Password };
            var loginResp = await client.PostAsync("/auth/login", new StringContent(JsonSerializer.Serialize(login), Encoding.UTF8, "application/json"));
            loginResp.EnsureSuccessStatusCode();
            var loginBody = await loginResp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(loginBody);
            var access = doc.RootElement.TryGetProperty("accessToken", out var at) ? at.GetString() : doc.RootElement.GetProperty("AccessToken").GetString();
            access.Should().NotBeNullOrEmpty();

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

            // Call warehouse
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
            var whResp = await client.GetAsync("/warehouse");
            whResp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            // Read full response as string before deserializing to avoid TestHost streaming compatibility issues
            var whBody = await whResp.Content.ReadAsStringAsync();
            whBody.Should().NotBeNullOrEmpty();

            // Try to extract the list of warehouses. The controller may wrap the list in an ApiResponse or return the list directly.
            List<WarehouseDto>? warehouses = null;
            try
            {
                // First, attempt to parse as ApiResponse<List<WarehouseDto>>
                var apiResp = JsonSerializer.Deserialize<Onion.Common.Models.ApiResponse<List<WarehouseDto>>>(whBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (apiResp != null && apiResp.Data != null)
                    warehouses = apiResp.Data;
            }
            catch { /* ignore and try fallback */ }

            if (warehouses == null)
            {
                // Fallback: try deserialize directly as a list
                warehouses = JsonSerializer.Deserialize<List<WarehouseDto>>(whBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            warehouses.Should().NotBeNull();
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
