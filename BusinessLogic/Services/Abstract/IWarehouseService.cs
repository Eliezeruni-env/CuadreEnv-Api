using System.Collections.Generic;
using System.Threading.Tasks;
using Onion.BussinesLogic.Dtos;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IWarehouseService
    {
        Task<Onion.Common.Models.Pagination.PagedList<Onion.BussinesLogic.Dtos.WarehouseDto>> GetPagedAsync(int pageNumber, int pageSize, int companyId);
        Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto dto, int companyId);
        Task<IEnumerable<WarehouseDto>> GetAllAsync(int companyId);
        Task<Onion.Common.Models.Pagination.PagedList<MovementDto>> GetMovementHistoryPagedAsync(int? productId = null, int? warehouseId = null, DateTime? from = null, DateTime? to = null, string? type = null, int pageNumber = 1, int pageSize = 10);
        Task<Onion.Common.Models.Pagination.PagedList<Onion.Domain.Inventory.InventoryMovement>> GetInventoryMovementsPagedAsync(int? productId = null, int? warehouseId = null, DateTime? from = null, DateTime? to = null, string? type = null, int pageNumber = 1, int pageSize = 10);
        Task AddStockAsync(MovementRequestDto req, string performedBy);
        Task RemoveStockAsync(MovementRequestDto req, string performedBy);
        Task TransferStockAsync(TransferRequestDto req, string performedBy);
        Task<IEnumerable<ProductDto>> GetLowStockAsync();
        Task<IEnumerable<MovementDto>> GetMovementHistoryAsync(int? productId = null, int? warehouseId = null, DateTime? from = null, DateTime? to = null, string? type = null);
        Task<IEnumerable<Onion.Domain.Inventory.InventoryMovement>> GetInventoryMovementsAsync(int? productId = null, int? warehouseId = null, DateTime? from = null, DateTime? to = null, string? type = null);
    }
}
