using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Onion.BussinesLogic.Services.Concrete;
using Xunit;

namespace Onion.Tests;

public sealed class CompanyEntitlementServiceTests
{
    [Fact]
    public async Task Reads_Wrapped_Usm_Entitlements_And_Normalizes_Module_Ids()
    {
        var handler = new StubHttpMessageHandler(
            """{"data":[{"moduleId":"fraud-guardian","enabled":true},{"moduleId":"admin-roles","enabled":true},{"moduleId":"billing","enabled":false}]}""",
            """{"data":{"projectIds":["project-one"]}}""");
        var factory = new StubHttpClientFactory(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://usm.test/")
        });
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Usm:BaseUrl"] = "https://usm.test",
                ["Usm:ApiKey"] = "shared-test-key"
            })
            .Build();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new CompanyEntitlementService(
            cache,
            factory,
            configuration,
            null!,
            NullLogger<CompanyEntitlementService>.Instance);

        var entitlements = await service.GetAsync(7);

        Assert.Contains("audit", entitlements.Modules);
        Assert.Contains("company", entitlements.Modules);
        Assert.DoesNotContain("billing", entitlements.Modules);
        Assert.Contains("project-one", entitlements.Projects);
        Assert.Equal(
            new[]
            {
                "https://usm.test/api/internal/companies/7/modules|shared-test-key",
                "https://usm.test/api/internal/companies/7/projects|shared-test-key"
            },
            handler.Requests);
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler(params string[] responses) : HttpMessageHandler
    {
        private int _responseIndex;
        public List<string> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add($"{request.RequestUri}|{request.Headers.GetValues("X-Api-Key").Single()}");
            var response = responses[_responseIndex++];
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            });
        }
    }
}
