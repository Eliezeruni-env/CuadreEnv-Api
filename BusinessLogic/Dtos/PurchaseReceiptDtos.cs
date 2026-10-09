using System;
using System.Collections.Generic;

namespace Onion.BussinesLogic.Dtos
{
    public class PurchaseOrderReceiptDto
    {
        public int PurchaseId { get; set; }
        public int WarehouseId { get; set; }
        public int SupplierId { get; set; }
        public DateTime ReceiptDate { get; set; }
        public List<PurchaseOrderReceiptDetailDto> Details { get; set; } = new List<PurchaseOrderReceiptDetailDto>();
    }

    public class PurchaseOrderReceiptDetailDto
    {
        public int ProductId { get; set; }
        public decimal QuantityReceived { get; set; }
        public decimal UnitCost { get; set; }
        public int WarehouseId { get; set; }
    }
}
