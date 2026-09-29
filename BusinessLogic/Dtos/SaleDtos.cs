using System;
using System.Collections.Generic;

namespace Onion.BussinesLogic.Dtos
{
    public class SaleDetailDto
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int? WarehouseId { get; set; }
    }

    public class SaleRequestDto
    {
        public int? CustomerId { get; set; }
        public decimal Total { get; set; }
        public decimal PaidAmount { get; set; }
        public int? CashRegisterId { get; set; }
        public DateTime? DueDate { get; set; }
        public List<SaleDetailDto> Details { get; set; } = new List<SaleDetailDto>();
    }
}
