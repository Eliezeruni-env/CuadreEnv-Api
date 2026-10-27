using System;
using System.Collections.Generic;

namespace Onion.Domain
{
    public class Company : BaseEntity
    {
        public string Name { get; set; }
        public string? Rnc { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public CompanySettings? Settings { get; set; }
    }

    public class Customer : BaseEntity
    {
        public string Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Identification { get; set; }
        public string? Notes { get; set; }
        public bool IsGeneric { get; set; }
        public decimal CurrentDebt { get; set; }
        public int CompanyId { get; set; }
    }

    public class Supplier : BaseEntity
    {
        public string Name { get; set; }
        public string? RncOrId { get; set; }
        public string? ContactName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
        public int CompanyId { get; set; }
    }

    public class Purchase : BaseEntity
    {
        public int SupplierId { get; set; }
        public decimal Total { get; set; }
        public int CompanyId { get; set; }
        public List<PurchaseDetail> Details { get; set; } = new List<PurchaseDetail>();
    }

    public class PurchaseDetail : BaseEntity
    {
        public int PurchaseId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        // Quantity physically received so far for this purchase detail
        public decimal QuantityReceived { get; set; }
        public decimal Cost { get; set; }
    }

    public enum SaleStatus { PENDING, PARTIAL, PAID, OVERDUE, CANCELLED }
    public enum PaymentType { CASH, CREDIT }
    public enum PaymentMethod
    {
        CASH = 0,
        EFECTIVO = CASH,
        TRANSFER = 1,
        TRANSFERENCIA = TRANSFER,
        CARD = 2,
        TARJETA = CARD,
        MULTIPLE = 3,
        CREDIT = 4,
        OTHER = 5
    }
    public enum SaleOriginChannel
    {
        ADMINISTRATIVO = 0,
        POS_PRESENCIAL = 1,
        CREDITO = 2,
        SERVICIO = 3,
        ONLINE = 4
    }

    public class Sale : BaseEntity
    {
        public int? CustomerId { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public decimal Total { get; set; }
        public decimal PaidAmount { get; set; }
        public SaleStatus Status { get; set; }
        public PaymentType PaymentType { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CASH;
        public DateTime? DueDate { get; set; }
        public int? CashRegisterId { get; set; }
        public int CompanyId { get; set; }
        public string InvoiceFolio { get; set; } = string.Empty;
        public List<SaleDetail> Details { get; set; } = new List<SaleDetail>();
        // POS / Caja fields
        public string? IdempotencyKey { get; set; }
        public string? ExternalReference { get; set; }
        public int? CashSessionId { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxWithheld { get; set; }
        public decimal LegalTip { get; set; }
        public Onion.Domain.Invoices.VoucherType VoucherType { get; set; }
        public SaleOriginChannel OriginChannel { get; set; } = SaleOriginChannel.ADMINISTRATIVO;
        public string? FiscalRnc { get; set; }
        public string? FiscalBusinessName { get; set; }
        public string? Notes { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsCancelled => Status == SaleStatus.CANCELLED;
        // Concurrency token
        public uint RowVersion { get; set; }
    }

    public class SaleDetail : BaseEntity
    {
        public int SaleId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        // Optional warehouse where the product will be taken from
        public int? WarehouseId { get; set; }
    }

    public class Payment : BaseEntity
    {
        public int? SaleId { get; set; }
        public int? AccountPayableId { get; set; }
        public int? AccountReceivableId { get; set; }
        public int CompanyId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? Reference { get; set; }
        public int? CashSessionId { get; set; }
        public decimal? AmountReceived { get; set; }
        public decimal? ChangeAmount { get; set; }
    }

    public class Return : BaseEntity
    {
        public int SaleId { get; set; }
        public decimal TotalReturned { get; set; }
        public int CompanyId { get; set; }
        public List<ReturnDetail> Details { get; set; } = new List<ReturnDetail>();
    }

    public class ReturnDetail : BaseEntity
    {
        public int ReturnId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class CashRegister : BaseEntity
    {
        public decimal OpeningAmount { get; set; }
        public decimal InitialAmount { get; set; }
        public decimal? ClosingAmount { get; set; }
        public decimal? DifferenceAmount { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public int CompanyId { get; set; }
        public bool IsOpen { get; set; }
        public CashRegisterStatus Status { get; set; } = CashRegisterStatus.CLOSED;
        public int? OpenedByUserId { get; set; }
        public int? ClosedByUserId { get; set; }
        public int? PausedByUserId { get; set; }
        public DateTime? PausedAt { get; set; }
        public string? PauseReason { get; set; }
        public decimal? ExpectedAmount { get; set; }
        public decimal? PhysicalCountAmount { get; set; }
        public string? PhysicalCountBreakdownJson { get; set; }
        public bool IsImmutable { get; private set; }
        public uint RowVersion { get; set; }

        public void Close(Onion.Domain.Finance.CashDiscrepancy discrepancy, int? userId, string? breakdownJson, DateTime closedAt)
        {
            if (Status == CashRegisterStatus.CLOSED || IsImmutable)
                throw new InvalidOperationException("Cash register is already closed.");

            ExpectedAmount = discrepancy.ExpectedAmount;
            PhysicalCountAmount = discrepancy.ActualAmount;
            DifferenceAmount = discrepancy.DifferenceAmount;
            ClosingAmount = discrepancy.ActualAmount;
            PhysicalCountBreakdownJson = breakdownJson;
            ClosedAt = closedAt;
            IsOpen = false;
            Status = CashRegisterStatus.CLOSED;
            ClosedByUserId = userId;
            IsImmutable = true;
        }
    }

    public enum CashRegisterStatus { OPEN, PAUSED, CLOSED }

    public class CashRegisterPause : BaseEntity
    {
        public int CashRegisterId { get; set; }
        public int CompanyId { get; set; }
        public int UserId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime PausedAt { get; set; }
        public DateTime? ResumedAt { get; set; }
    }

    public class CashMovement : BaseEntity
    {
        public int CashRegisterId { get; set; }
        public int? CashSessionId { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public int CompanyId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int? ApprovedByUserId { get; set; }
        public Onion.Domain.Finance.CashMovementType Type { get; set; } = Onion.Domain.Finance.CashMovementType.CashIn;
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public uint RowVersion { get; set; }
    }

    public class CompanySettings : BaseEntity
    {
        public int CompanyId { get; set; }
        public virtual Company? Company { get; set; }
        public int CreditDaysLimit { get; set; }
        public bool BlockSalesIfOverdue { get; set; }
        // Module 10 settings
        public string Currency { get; set; } = string.Empty;
        public string TimeZone { get; set; } = string.Empty;
        public string InvoiceNumberFormat { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? CommercialName { get; set; }
        public int DefaultStockAlertThreshold { get; set; }
        public decimal DefaultTaxPercentage { get; set; }
        public decimal CashToleranceAmount { get; set; }
        public bool AllowNegativeStock { get; set; }
        public string? InvoiceFooterPhrase { get; set; }
        public string? TicketPaperSize { get; set; } = "80mm";
        public string? TicketInstagram { get; set; }
        public string? TicketWhatsapp { get; set; }
        public bool TicketShowQr { get; set; } = true;
    }
}
