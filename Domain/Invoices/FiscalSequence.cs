using Onion.Domain;

namespace Onion.Domain.Invoices;

public class FiscalSequence : BaseEntity
{
    public int CompanyId { get; set; }
    public VoucherType VoucherType { get; set; }
    public string Prefix { get; set; } = string.Empty;
    public long CurrentNumber { get; set; }
    public long FromNumber { get; set; }
    public long ToNumber { get; set; }
    public DateTime ExpirationDate { get; set; }
    public int WarningThreshold { get; set; } = 50;
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public long Remaining => Math.Max(0, ToNumber - CurrentNumber + 1);
    public bool IsElectronic => Prefix.StartsWith("E", StringComparison.OrdinalIgnoreCase);

    public void Validate()
    {
        if (CompanyId <= 0) throw new ArgumentOutOfRangeException(nameof(CompanyId));
        if (string.IsNullOrWhiteSpace(Prefix)) throw new ArgumentException("Prefix is required.", nameof(Prefix));
        if (FromNumber < 0 || ToNumber < FromNumber) throw new ArgumentException("Invalid fiscal range.");
        if (CurrentNumber < FromNumber) CurrentNumber = FromNumber;
        if (WarningThreshold < 0) throw new ArgumentOutOfRangeException(nameof(WarningThreshold));
    }
}
