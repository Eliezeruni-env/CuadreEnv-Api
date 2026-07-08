using System;
using Onion.Domain;

namespace Onion.Domain.Finance
{
    public enum ReceivableStatus { Open, Paid, Overdue }

    public class AccountReceivable : BaseEntity
    {
        public int CompanyId { get; set; }
        public int? CustomerId { get; set; }
        public int? SaleId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance => TotalAmount - PaidAmount;
        public DateTime DueDate { get; set; }
        public ReceivableStatus Status { get; set; }
    }
}
