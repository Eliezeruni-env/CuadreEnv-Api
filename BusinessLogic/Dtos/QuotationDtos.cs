using System;
using System.Collections.Generic;
using Onion.Domain.Quotations;

namespace Onion.BussinesLogic.Dtos
{
    public class QuotationDetailDto
    {
        public int Id { get; set; }
        public int QuotationId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductCode { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
    }

    public class QuotationDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string QuotationNumber { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ValidUntil { get; set; }
        public string PaymentTerms { get; set; } = "Contado";
        public string? Salesperson { get; set; }
        public string? Observations { get; set; }
        public QuotationStatus Status { get; set; }
        public string StatusName => Status switch
        {
            QuotationStatus.DRAFT => "Borrador",
            QuotationStatus.SENT => "Enviada",
            QuotationStatus.APPROVED => "Aprobada",
            QuotationStatus.CONVERTED => "Convertida",
            QuotationStatus.EXPIRED => "Vencida",
            _ => "Desconocido"
        };
        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal Total { get; set; }
        public int? ConvertedSaleId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<QuotationDetailDto> Details { get; set; } = new List<QuotationDetailDto>();
    }

    public class CreateQuotationDetailDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductCode { get; set; }
        public decimal Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TaxPercent { get; set; } = 18m;
    }

    public class CreateQuotationDto
    {
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string PaymentTerms { get; set; } = "Contado";
        public string? Salesperson { get; set; }
        public string? Observations { get; set; }
        public QuotationStatus Status { get; set; } = QuotationStatus.DRAFT;
        public List<CreateQuotationDetailDto> Details { get; set; } = new List<CreateQuotationDetailDto>();
    }

    public class UpdateQuotationStatusDto
    {
        public QuotationStatus Status { get; set; }
    }

    public class QuotationSummaryKpiDto
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int SentCount { get; set; }
        public int PendingCount { get; set; } // Draft + Sent
        public int ApprovedCount { get; set; }
        public int ConvertedCount { get; set; }
        public int ExpiredCount { get; set; }
    }

    public class QuotationListResponseDto
    {
        public QuotationSummaryKpiDto Kpis { get; set; } = new QuotationSummaryKpiDto();
        public List<QuotationDto> Items { get; set; } = new List<QuotationDto>();
        public int TotalItems { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
