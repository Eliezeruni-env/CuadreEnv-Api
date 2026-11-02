using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Dtos;
using Microsoft.AspNetCore.Authorization;
using Onion.Common.Authorization;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    [AuthorizeModule("INVENTORY")]
    public class InventoryController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public InventoryController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [HttpPost("inbound")]
        [AuthorizeExactModule("inventory-entries")]
        public async Task<IActionResult> Inbound([FromBody] MovementRequestDto req)
        {
            return Ok(await _warehouseService.AddStockAsync(req, User?.Identity?.Name ?? "system"));
        }

        [HttpPost("outbound")]
        public async Task<IActionResult> Outbound([FromBody] MovementRequestDto req)
        {
            return Ok(await _warehouseService.RemoveStockAsync(req, User?.Identity?.Name ?? "system"));
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequestDto req)
        {
            return Ok(await _warehouseService.TransferStockAsync(req, User?.Identity?.Name ?? "system"));
        }

        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock()
        {
            var items = await _warehouseService.GetLowStockAsync();
            return Ok(items);
        }

        [HttpGet("movements")]
        public async Task<IActionResult> GetMovements([FromQuery] int? productId = null, [FromQuery] int? warehouseId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string? type = null, [FromQuery] int? pageNumber = null, [FromQuery] int? pageSize = null)
        {
            if (pageNumber.HasValue)
            {
                var paged = await _warehouseService.GetMovementHistoryPagedAsync(productId, warehouseId, from, to, type, pageNumber.Value, pageSize ?? 10);
                return Ok(paged);
            }

            var items = await _warehouseService.GetMovementHistoryAsync(productId, warehouseId, from, to, type);
            return Ok(items);
        }

        [HttpGet("audit-movements")]
        public async Task<IActionResult> GetAuditMovements([FromQuery] int? productId = null, [FromQuery] int? warehouseId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string? type = null)
        {
            var items = await _warehouseService.GetInventoryMovementsAsync(productId, warehouseId, from, to, type);
            return Ok(items);
        }
    }
}
