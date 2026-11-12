using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Onion.DataAccess;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain.Invoices;

namespace Onion.BussinesLogic.Background;

public sealed class FiscalOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FiscalOutboxWorker> _logger;
    private readonly IFiscalSubmissionProvider _submissionProvider;

    public FiscalOutboxWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<FiscalOutboxWorker> logger,
        IFiscalSubmissionProvider submissionProvider)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _submissionProvider = submissionProvider;
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
        if (string.IsNullOrWhiteSpace(_configuration.GetConnectionString("OnionCrud")))
        {
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OnionDbContext>();
        var now = DateTime.UtcNow;
        var processingTimeout = TimeSpan.FromSeconds(Math.Clamp(
            _configuration.GetValue("Fiscal:ProcessingTimeoutSeconds", 900), 60, 86400));
        var staleProcessingAt = now - processingTimeout;
        var candidates = await db.FiscalDocuments
            .IgnoreQueryFilters()
            .Where(x => x.Status == FiscalDocumentStatus.Pending ||
                        ((x.Status == FiscalDocumentStatus.Retry || x.Status == FiscalDocumentStatus.RetryScheduled) && x.NextAttemptAt <= now) ||
                        (x.Status == FiscalDocumentStatus.Processing && x.LastAttemptAt <= staleProcessingAt))
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
                             ((x.Status == FiscalDocumentStatus.Retry || x.Status == FiscalDocumentStatus.RetryScheduled) && x.NextAttemptAt <= now) ||
                             (x.Status == FiscalDocumentStatus.Processing && x.LastAttemptAt <= staleProcessingAt)))
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
        Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = document.CompanyId;
        try
        {
            string responseBody;
            try
            {
                var result = await _submissionProvider.SubmitAsync(document, cancellationToken);
                responseBody = result.ResponsePayload;
                if (result.Outcome == FiscalSubmissionOutcome.Rejected)
                {
                    document.Status = FiscalDocumentStatus.RejectedByFiscalProvider;
                    document.LastError = result.ErrorMessage;
                    document.NextAttemptAt = null;
                    db.FiscalSubmissionAudits.Add(new FiscalSubmissionAudit
                    {
                        CompanyId = document.CompanyId,
                        FiscalDocumentId = document.Id,
                        EventType = "RejectedByFiscalProvider",
                        ResponsePayload = responseBody
                    });
                    await db.SaveChangesAsync(cancellationToken);
                    return;
                }

                if (result.Outcome == FiscalSubmissionOutcome.Retry)
                    throw new HttpRequestException(result.ErrorMessage ?? "Fiscal provider requested a retry.");

                document.Status = _configuration.GetValue<bool>("Dgii:Enabled")
                    ? FiscalDocumentStatus.AcceptedByDgii
                    : FiscalDocumentStatus.AcceptedByMock;
                document.LastError = null;
                document.NextAttemptAt = null;
                db.FiscalSubmissionAudits.Add(new FiscalSubmissionAudit
                {
                    CompanyId = document.CompanyId,
                    FiscalDocumentId = document.Id,
                    EventType = _configuration.GetValue<bool>("Dgii:Enabled") ? "AcceptedByDgii" : "AcceptedByMock",
                    ResponsePayload = responseBody
                });
            }
            catch (Exception ex) when ((ex is HttpRequestException || ex is TaskCanceledException) && !cancellationToken.IsCancellationRequested)
            {
                var maxAttempts = Math.Max(1, _configuration.GetValue("Fiscal:MaxAttempts", 8));
                var exhausted = document.AttemptCount >= maxAttempts;
                document.Status = exhausted ? FiscalDocumentStatus.RejectedByFiscalProvider : FiscalDocumentStatus.RetryScheduled;
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
        finally
        {
            Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = null;
        }
    }
}
