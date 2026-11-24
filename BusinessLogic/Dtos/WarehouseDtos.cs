namespace Onion.BussinesLogic.Dtos
{
    public class WarehouseMovementDetailDto
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? BarCode { get; set; }
        public decimal Quantity { get; set; }
        public decimal Cost { get; set; }
        public decimal UnitCost { get; set; }
    }

    public record WarehouseDto(int Id, string Name, int CompanyId, string Code = "");
    public record CreateWarehouseDto(string Name, string? Code = null);

    public record InventoryDto(int ProductId, int WarehouseId, decimal Quantity);
    public record WarehouseStockDto(int ProductId, decimal Stock);

    public record MovementDto(
        int Id,
        int ProductId,
        int? FromWarehouseId,
        int? ToWarehouseId,
        decimal Quantity,
        string Type,
        string? ProductDescription = null,
        string? ProductBarcode = null,
        string? ProductReference = null,
        string? FromWarehouseName = null,
        string? ToWarehouseName = null,
        DateTime? CreationDate = null,
        decimal UnitCost = 0,
        decimal TotalCost = 0,
        string? MovementNumber = null);

    public record MovementRequestDto(int ProductId, int WarehouseId, decimal Quantity, bool? IsPackage = false, decimal? PackageFactor = null)
    {
        public List<WarehouseMovementDetailDto> ProductDetails { get; init; } = new();
        public List<WarehouseMovementDetailDto> Details { get; init; } = new();
        public decimal TotalQuantity { get; init; }
        public decimal Cost { get; init; }
        public decimal TotalCost { get; init; }
        public string? MovementNumber { get; init; }
    }
    public record InventoryMovementRequestDto(int ProductId, int WarehouseId, decimal Quantity, string Type = "Entrada", bool? IsPackage = false, decimal? PackageFactor = null);
    public record TransferRequestDto(int ProductId, int FromWarehouseId, int ToWarehouseId, decimal Quantity)
    {
        public List<WarehouseMovementDetailDto> ProductDetails { get; init; } = new();
        public List<WarehouseMovementDetailDto> Details { get; init; } = new();
        public decimal TotalQuantity { get; init; }
        public decimal Cost { get; init; }
        public decimal TotalCost { get; init; }
        public string? MovementNumber { get; init; }
    }
}
