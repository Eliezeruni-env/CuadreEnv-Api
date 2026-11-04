using Onion.Domain.Invoices;

namespace Onion.BussinesLogic.Services.Abstract;

public interface IFiscalSubmissionProvider
{
    Task<FiscalSubmissionResult> SubmitAsync(FiscalDocument document, CancellationToken cancellationToken);
}

public sealed record FiscalSubmissionResult(
    FiscalSubmissionOutcome Outcome,
    string ResponsePayload,
    string? ErrorMessage = null,
    int? StatusCode = null);

public enum FiscalSubmissionOutcome
{
    Accepted,
    Rejected,
    Retry
}
