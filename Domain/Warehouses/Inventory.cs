using Onion.Domain;

namespace Onion.Domain.Warehouses
{
    public class Inventory : BaseEntity
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public decimal Quantity { get; set; }
        public int CompanyId { get; set; }
    }
}
