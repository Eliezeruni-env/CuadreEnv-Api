namespace Onion.Common.Services;

public interface ISecurityAlertPublisher
{
    Task PublishAsync(string eventCode, int companyId, object data, CancellationToken cancellationToken = default);
}

public static class SecurityAlertEvents
{
    public const string DrawerOpenedNoSale = "DRAWER_OPENED_NO_SALE";
    public const string InvoiceCancelAttempt = "INVOICE_CANCEL_ATTEMPT";
    public const string HighCashDrop = "HIGH_CASH_DROP";
    public const string OutOfToleranceClose = "OUT_OF_TOLERANCE_CLOSE";
}
