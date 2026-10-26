namespace Onion.BussinesLogic.Dtos
{
    public record WarehouseDto(int Id, string Name, int CompanyId);
    public record CreateWarehouseDto(string Name);

    public record InventoryDto(int ProductId, int WarehouseId, decimal Quantity);

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
        DateTime? CreationDate = null);

    public record MovementRequestDto(int ProductId, int WarehouseId, decimal Quantity, bool? IsPackage = false, decimal? PackageFactor = null);
    public record TransferRequestDto(int ProductId, int FromWarehouseId, int ToWarehouseId, decimal Quantity);
}
