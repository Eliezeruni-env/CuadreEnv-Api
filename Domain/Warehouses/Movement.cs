using Onion.Domain;

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
        public MovementType Type { get; set; }
        public int CompanyId { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
