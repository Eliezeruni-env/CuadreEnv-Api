using System;
using System.Collections.Generic;

namespace Onion.BussinesLogic.Dtos
{
    public class SaleItemDto
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class SaleDto
    {
        public string IdempotencyKey { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
        public string? CreateBy { get; set; }
        public int? CashRegisterId { get; set; }
        public int? CashSessionId { get; set; }
        public string? OriginChannel { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentReference { get; set; }
        public List<SaleItemDto> Items { get; set; } = new List<SaleItemDto>();
    }
}
