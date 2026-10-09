using System;
using System.Collections.Generic;
using Onion.Domain;

namespace Onion.Domain.Purchases
{
    public class PurchaseOrderReceipt : BaseEntity
    {
        public int PurchaseId { get; set; }
        public int WarehouseId { get; set; }
        public int SupplierId { get; set; }
        public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
        public int StatusId { get; set; }
        public List<PurchaseOrderReceiptDetail> Details { get; set; } = new List<PurchaseOrderReceiptDetail>();
        public int CompanyId { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class PurchaseOrderReceiptDetail : BaseEntity
    {
        public int PurchaseOrderReceiptId { get; set; }
        public int ProductId { get; set; }
        public decimal QuantityReceived { get; set; }
        public decimal UnitCost { get; set; }
        public int WarehouseId { get; set; }
        public int CompanyId { get; set; }
    }
}
