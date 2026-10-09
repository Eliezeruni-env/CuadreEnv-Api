using System;
using Onion.Domain;

namespace Onion.Domain.Inventory
{
    public enum MovementType { In, Out, Adjustment, Sale, Purchase, Return }

    public class InventoryMovement : BaseEntity
    {
        public int CompanyId { get; set; }
        public MovementType Type { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal CostUnit { get; set; }
        public decimal BalanceStock { get; set; }
        public decimal BalanceCost { get; set; }
        public int WarehouseId { get; set; }
        public int PerformedByUserId { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public string Reference { get; set; } = string.Empty; // e.g. SaleId
        public string Comment { get; set; } = string.Empty;
    }
}
