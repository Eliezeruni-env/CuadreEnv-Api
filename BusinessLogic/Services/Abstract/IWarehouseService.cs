using System.Collections.Generic;
using System.Threading.Tasks;
using Onion.BussinesLogic.Dtos;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IWarehouseService
    {
        Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto dto, int companyId);
        Task<IEnumerable<WarehouseDto>> GetAllAsync();
        Task AddStockAsync(MovementRequestDto req, string performedBy);
        Task RemoveStockAsync(MovementRequestDto req, string performedBy);
        Task TransferStockAsync(TransferRequestDto req, string performedBy);
    }
}
