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
    public enum PaymentMethod { CASH, TRANSFER, CARD, OTHER }

    public class Sale : BaseEntity
    {
        public int? CustomerId { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public decimal Total { get; set; }
        public decimal PaidAmount { get; set; }
        public SaleStatus Status { get; set; }
        public PaymentType PaymentType { get; set; }
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
        public decimal Tax { get; set; }
        public string? Notes { get; set; }
        // Concurrency token
        public byte[]? RowVersion { get; set; }
    }

    public class SaleDetail : BaseEntity
    {
        public int SaleId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
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
        public decimal? ClosingAmount { get; private set; }
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
        public decimal? ExpectedAmount { get; private set; }
        public decimal? PhysicalCountAmount { get; private set; }
        public string? PhysicalCountBreakdownJson { get; private set; }
        public bool IsImmutable { get; private set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

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
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public int CompanyId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int? ApprovedByUserId { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public class CompanySettings : BaseEntity
    {
        public int CompanyId { get; set; }
        public virtual Company? Company { get; set; }
        public int CreditDaysLimit { get; set; } = 30;
        public bool BlockSalesIfOverdue { get; set; }
        // Module 10 settings
        public string Currency { get; set; } = "USD";
        public string TimeZone { get; set; } = "UTC";
        public string InvoiceNumberFormat { get; set; } = "{company}-{sequential}";
        public string? LogoUrl { get; set; }
        public string? CommercialName { get; set; }
        public int DefaultStockAlertThreshold { get; set; } = 5;
        public decimal DefaultTaxPercentage { get; set; } = 0m;
    }
}
