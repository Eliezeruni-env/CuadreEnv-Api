using Onion.Domain;

namespace Onion.Domain.Invoices
{
    public class InvoiceSequence : BaseEntity
    {
        public int CompanyId { get; set; }
        public long LastFolio { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
