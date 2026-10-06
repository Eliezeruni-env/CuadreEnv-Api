using Onion.Domain;

namespace Onion.Domain.Finance;

public enum TaxpayerStatus
{
    Active = 1,
    Suspended = 2
}

public class Taxpayer : BaseEntity
{
    public string RncOrCedula { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string? CommercialName { get; set; }
    public string? Category { get; set; }
    public TaxpayerStatus Status { get; set; } = TaxpayerStatus.Active;
    public DateTime LastSynchronizedAt { get; set; } = DateTime.UtcNow;
}
