using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Onion.DataAccess;
using Onion.Domain.Invoices;

namespace Onion.BussinesLogic.Background;

public sealed class FiscalOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FiscalOutboxWorker> _logger;

    public FiscalOutboxWorker(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<FiscalOutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Clamp(_configuration.GetValue("Fiscal:PollSeconds", 5), 1, 300));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fiscal outbox batch failed");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["Dgii:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OnionDbContext>();
        var now = DateTime.UtcNow;
        var candidates = await db.FiscalDocuments
            .IgnoreQueryFilters()
            .Where(x => x.Status == FiscalDocumentStatus.Pending ||
                        (x.Status == FiscalDocumentStatus.Retry && x.NextAttemptAt <= now))
            .OrderBy(x => x.CreationDate)
            .Select(x => x.Id)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var id in candidates)
        {
            var claimed = await db.FiscalDocuments
                .IgnoreQueryFilters()
                .Where(x => x.Id == id &&
                            (x.Status == FiscalDocumentStatus.Pending ||
                             (x.Status == FiscalDocumentStatus.Retry && x.NextAttemptAt <= now)))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, FiscalDocumentStatus.Processing)
                    .SetProperty(x => x.LastAttemptAt, now)
                    .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1), cancellationToken);
            if (claimed == 0)
                continue;

            await SubmitAsync(db, id, cancellationToken);
        }
    }

    private async Task SubmitAsync(OnionDbContext db, int id, CancellationToken cancellationToken)
    {
        var document = await db.FiscalDocuments.IgnoreQueryFilters().SingleAsync(x => x.Id == id, cancellationToken);
        var client = _httpClientFactory.CreateClient("Dgii");
        using var request = new HttpRequestMessage(HttpMethod.Post,
            (_configuration["Dgii:SubmitPath"] ?? "api/fiscal/submit").TrimStart('/'))
        {
            Content = JsonContent.Create(new { documentKey = document.DocumentKey, saleId = document.SaleId, ncf = document.Ncf })
        };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", document.DocumentKey);
        var apiKey = _configuration["Dgii:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);

        string responseBody;
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                if (statusCode is not (408 or 429) && statusCode >= 400 && statusCode < 500)
                {
                    document.Status = FiscalDocumentStatus.Rejected;
                    document.LastError = $"DGII returned {statusCode} {response.StatusCode}.";
                    document.NextAttemptAt = null;
                    db.FiscalSubmissionAudits.Add(new FiscalSubmissionAudit
                    {
                        CompanyId = document.CompanyId,
                        FiscalDocumentId = document.Id,
                        EventType = "RejectedByDgii",
                        ResponsePayload = responseBody
                    });
                    await db.SaveChangesAsync(cancellationToken);
                    return;
                }

                throw new HttpRequestException($"DGII returned {statusCode} {response.StatusCode}.");
            }

            document.Status = FiscalDocumentStatus.Accepted;
            document.LastError = null;
            document.NextAttemptAt = null;
            db.FiscalSubmissionAudits.Add(new FiscalSubmissionAudit
            {
                CompanyId = document.CompanyId,
                FiscalDocumentId = document.Id,
                EventType = "Accepted",
                ResponsePayload = responseBody
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            var maxAttempts = Math.Max(1, _configuration.GetValue("Fiscal:MaxAttempts", 8));
            var exhausted = document.AttemptCount >= maxAttempts;
            document.Status = exhausted ? FiscalDocumentStatus.Rejected : FiscalDocumentStatus.Retry;
            document.LastError = ex.Message;
            document.NextAttemptAt = exhausted ? null : DateTime.UtcNow.AddSeconds(Math.Min(3600, Math.Pow(2, document.AttemptCount)));
            responseBody = ex.Message;
            db.FiscalSubmissionAudits.Add(new FiscalSubmissionAudit
            {
                CompanyId = document.CompanyId,
                FiscalDocumentId = document.Id,
                EventType = exhausted ? "RejectedAfterRetries" : "RetryScheduled",
                ResponsePayload = responseBody
            });
            _logger.LogWarning(ex, "Fiscal submission failed for document {DocumentId}; status {Status}", id, document.Status);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
