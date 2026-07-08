using System;
using System.Collections.Generic;

namespace Onion.Domain
{
    public class Company : BaseEntity
    {
        public string Name { get; set; }
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
        public string? Phone { get; set; }
        public string? Email { get; set; }
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
    }

    public class SaleDetail : BaseEntity
    {
        public int SaleId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class Payment : BaseEntity
    {
        public int SaleId { get; set; }
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
        public decimal? ClosingAmount { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public int CompanyId { get; set; }
        public bool IsOpen { get; set; }
    }

    public class CashMovement : BaseEntity
    {
        public int CashRegisterId { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public int CompanyId { get; set; }
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
