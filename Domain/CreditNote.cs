using System;
using System.Collections.Generic;

namespace Onion.Domain
{
    public class CreditNote : BaseEntity
    {
        public string Number { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public int CompanyId { get; set; }
        public int CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Reason { get; set; }
        public List<CreditNoteDetail> Details { get; set; } = new List<CreditNoteDetail>();
    }

    public class CreditNoteDetail : BaseEntity
    {
        public int CreditNoteId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;
    }
}
