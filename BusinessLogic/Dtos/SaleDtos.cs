using System;
using System.Collections.Generic;

namespace Onion.BussinesLogic.Dtos
{
    public class SaleDetailDto
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public int? WarehouseId { get; set; }
    }

    public class SaleRequestDto
    {
        public int? CustomerId { get; set; }
        public decimal Total { get; set; }
        public decimal PaidAmount { get; set; }
        public int? CashRegisterId { get; set; }
        public int? CashSessionId { get; set; }
        public Onion.Domain.SaleOriginChannel OriginChannel { get; set; } = Onion.Domain.SaleOriginChannel.ADMINISTRATIVO;
        public Onion.Domain.Invoices.VoucherType VoucherType { get; set; } = Onion.Domain.Invoices.VoucherType.B02;
        public Onion.Domain.PaymentType PaymentType { get; set; } = Onion.Domain.PaymentType.CASH;
        public Onion.Domain.PaymentMethod PaymentMethod { get; set; } = Onion.Domain.PaymentMethod.CASH;
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal TaxWithheld { get; set; }
        public decimal LegalTip { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Notes { get; set; }
        public string? FiscalRnc { get; set; }
        public string? FiscalBusinessName { get; set; }
        public PaymentRequestDto? Payment { get; set; }
        public List<PaymentComponentDto> Payments { get; set; } = new();
        public string? IdempotencyKey { get; set; }
        public List<SaleDetailDto> Details { get; set; } = new List<SaleDetailDto>();
    }

    public class PaymentRequestDto
    {
        public Onion.Domain.PaymentMethod Method { get; set; }
        public decimal? AmountReceived { get; set; }
        public decimal? ChangeAmount { get; set; }
        public string? Reference { get; set; }
    }

    public class PaymentComponentDto
    {
        public Onion.Domain.PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }
        public decimal? AmountReceived { get; set; }
        public decimal? ChangeAmount { get; set; }
        public string? Reference { get; set; }
    }
}
