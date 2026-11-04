using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain.Invoices;

namespace Onion.BussinesLogic.Services.Concrete;

public sealed class DgiiFiscalSubmissionProvider : IFiscalSubmissionProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public DgiiFiscalSubmissionProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;

        if (string.IsNullOrWhiteSpace(configuration["Dgii:BaseUrl"]))
            throw new InvalidOperationException("Dgii:BaseUrl must be configured when Dgii:Enabled=true.");
    }

    public async Task<FiscalSubmissionResult> SubmitAsync(FiscalDocument document, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("Dgii");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            (_configuration["Dgii:SubmitPath"] ?? "api/fiscal/submit").TrimStart('/'))
        {
            Content = JsonContent.Create(new
            {
                documentKey = document.DocumentKey,
                saleId = document.SaleId,
                ncf = document.Ncf
            })
        };

        request.Headers.TryAddWithoutValidation("Idempotency-Key", document.DocumentKey);
        var apiKey = _configuration["Dgii:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);

        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
            return new FiscalSubmissionResult(FiscalSubmissionOutcome.Accepted, responseBody, StatusCode: (int)response.StatusCode);

        var statusCode = (int)response.StatusCode;
        var isPermanentClientError = statusCode is not (408 or 429) && statusCode >= 400 && statusCode < 500;
        return new FiscalSubmissionResult(
            isPermanentClientError ? FiscalSubmissionOutcome.Rejected : FiscalSubmissionOutcome.Retry,
            responseBody,
            $"DGII returned {statusCode} {response.StatusCode}.",
            statusCode);
    }
}
