using System.Text.Json;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain.Invoices;

namespace Onion.BussinesLogic.Services.Concrete;

public sealed class MockFiscalSubmissionProvider : IFiscalSubmissionProvider
{
    public Task<FiscalSubmissionResult> SubmitAsync(FiscalDocument document, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = JsonSerializer.Serialize(new
        {
            provider = "mock",
            accepted = true,
            documentKey = document.DocumentKey,
            saleId = document.SaleId,
            ncf = document.Ncf,
            processedAtUtc = DateTime.UtcNow
        });

        return Task.FromResult(new FiscalSubmissionResult(
            FiscalSubmissionOutcome.Accepted,
            payload));
    }
}
