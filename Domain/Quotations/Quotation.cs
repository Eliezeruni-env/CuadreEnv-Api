using System;
using System.Collections.Generic;

namespace Onion.Domain.Quotations
{
    public enum QuotationStatus
    {
        DRAFT = 0,      // Borrador
        SENT = 1,       // Enviada
        APPROVED = 2,   // Aprobada
        CONVERTED = 3,  // Convertida
        EXPIRED = 4     // Vencida
    }

    public class Quotation : BaseEntity
    {
        public int CompanyId { get; set; }
        public string QuotationNumber { get; set; } = string.Empty; // e.g. COT-00028
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.UtcNow; // Fecha de emisión
        public DateTime ValidUntil { get; set; } // Válida hasta
        public string PaymentTerms { get; set; } = "Contado"; // Condiciones de pago (Contado, Crédito, etc.)
        public string? Salesperson { get; set; } // Vendedor
        public string? Observations { get; set; } // Observaciones
        public QuotationStatus Status { get; set; } = QuotationStatus.DRAFT;

        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxTotal { get; set; } // ITBIS (18%)
        public decimal Total { get; set; }

        public int? ConvertedSaleId { get; set; } // ID de la venta creada si fue convertida
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public List<QuotationDetail> Details { get; set; } = new List<QuotationDetail>();
    }

    public class QuotationDetail : BaseEntity
    {
        public int QuotationId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductCode { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxPercent { get; set; } = 18m;
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
    }
}
