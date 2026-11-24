using Onion.Domain;
using System.Collections.Generic;

namespace Onion.Domain.Warehouses
{
    public enum MovementType
    {
        Inbound = 0,
        Outbound = 1,
        Transfer = 2
    }

    public class Movement : BaseEntity
    {
        public int ProductId { get; set; }
        public int? FromWarehouseId { get; set; }
        public int? ToWarehouseId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string? MovementNumber { get; set; }
        public List<MovementDetail> Details { get; set; } = new();
        public MovementType Type { get; set; }
        public int CompanyId { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class MovementDetail : BaseEntity
    {
        public int MovementId { get; set; }
        public Movement Movement { get; set; } = null!;
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? BarCode { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
    }
}
