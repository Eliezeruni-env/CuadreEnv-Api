using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public InventoryController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [HttpPost("inbound")]
        public async Task<IActionResult> Inbound([FromBody] MovementRequestDto req)
        {
            await _warehouseService.AddStockAsync(req, User?.Identity?.Name ?? "system");
            return Ok();
        }

        [HttpPost("outbound")]
        public async Task<IActionResult> Outbound([FromBody] MovementRequestDto req)
        {
            await _warehouseService.RemoveStockAsync(req, User?.Identity?.Name ?? "system");
            return Ok();
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequestDto req)
        {
            await _warehouseService.TransferStockAsync(req, User?.Identity?.Name ?? "system");
            return Ok();
        }
    }
}
