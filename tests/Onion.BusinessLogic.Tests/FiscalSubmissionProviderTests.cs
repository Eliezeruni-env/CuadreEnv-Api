using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Services.Concrete;
using Onion.Domain.Invoices;
using Xunit;

namespace Onion.BusinessLogic.Tests;

public sealed class FiscalSubmissionProviderTests
{
    [Fact]
    public async Task MockProvider_AcceptsDocumentWithoutExternalConfiguration()
    {
        var provider = new MockFiscalSubmissionProvider();
        var document = FiscalDocument.ForSale(10, 25, "E-CF-1");

        var result = await provider.SubmitAsync(document, CancellationToken.None);

        Assert.Equal(FiscalSubmissionOutcome.Accepted, result.Outcome);
        Assert.Contains("\"provider\":\"mock\"", result.ResponsePayload);
        Assert.Contains(document.DocumentKey, result.ResponsePayload);
    }

    [Fact]
    public void DgiiProvider_RequiresBaseUrl()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        Assert.Throws<InvalidOperationException>(() =>
            new DgiiFiscalSubmissionProvider(new StubHttpClientFactory(), configuration));
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
