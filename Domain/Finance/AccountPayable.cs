using System;
using Onion.Domain;

namespace Onion.Domain.Finance;

public enum PayableStatus { Open, Paid, Overdue }

public class AccountPayable : BaseEntity
{
    public int CompanyId { get; set; }
    public int SupplierId { get; set; }
    public int? PurchaseId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance => TotalAmount - PaidAmount;
    public DateTime DueDate { get; set; }
    public PayableStatus Status { get; set; }
}