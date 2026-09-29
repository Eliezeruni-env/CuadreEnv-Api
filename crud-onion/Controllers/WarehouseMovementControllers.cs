using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Dtos;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class WarehouseEntryController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehouseEntryController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? warehouseId = null, [FromQuery] int? productId = null)
        {
            var movements = await _warehouseService.GetMovementHistoryAsync(productId, warehouseId, type: "Inbound");
            return Ok(movements);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MovementRequestDto dto)
        {
            if (dto == null) return BadRequest();
            await _warehouseService.AddStockAsync(dto, User?.Identity?.Name ?? "system");
            return Ok(new { success = true, data = dto });
        }
    }

    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class WarehouseOutletController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehouseOutletController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? warehouseId = null, [FromQuery] int? productId = null)
        {
            var movements = await _warehouseService.GetMovementHistoryAsync(productId, warehouseId, type: "Outbound");
            return Ok(movements);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MovementRequestDto dto)
        {
            if (dto == null) return BadRequest();
            await _warehouseService.RemoveStockAsync(dto, User?.Identity?.Name ?? "system");
            return Ok(new { success = true, data = dto });
        }
    }

    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class WarehouseTransferController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehouseTransferController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? warehouseId = null, [FromQuery] int? productId = null)
        {
            var movements = await _warehouseService.GetMovementHistoryAsync(productId, warehouseId, type: "Transfer");
            return Ok(movements);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TransferRequestDto dto)
        {
            if (dto == null) return BadRequest();
            await _warehouseService.TransferStockAsync(dto, User?.Identity?.Name ?? "system");
            return Ok(new { success = true, data = dto });
        }
    }
}
