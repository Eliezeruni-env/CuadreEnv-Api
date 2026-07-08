using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Dtos;
using Microsoft.AspNetCore.Authorization;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using Onion.Common.Enums;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;
        private readonly IGlobalizationService _globalizationService;

        public WarehouseController(IWarehouseService warehouseService, IGlobalizationService globalizationService)
        {
            _warehouseService = warehouseService;
            _globalizationService = globalizationService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateWarehouseDto dto)
        {
            // Extract company id from authenticated user claims
            var companyIdClaim = User?.FindFirst("CompanyId")?.Value;
            if (!int.TryParse(companyIdClaim, out var companyId))
            {
                return BadRequest(Onion.Common.Models.ApiResponse<string>.Fail("CompanyId claim missing or invalid"));
            }

            var created = await _warehouseService.CreateWarehouseAsync(dto, companyId);
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, Onion.Common.Models.ApiResponse<Onion.BussinesLogic.Dtos.WarehouseDto>.Ok(created, "Warehouse created"));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _warehouseService.GetAllAsync();
            if (!list.Any())
            {
                throw new HttpResponseException
                {
                    Errors = new Onion.Common.Models.Error[] { _globalizationService.GetErrorInCurrentLanguage(ErrorCodes.WarehousesNotFound) },
                    StatusCode = System.Net.HttpStatusCode.BadRequest
                };
            }
            try
            {
                return Ok(Onion.Common.Models.ApiResponse<object>.Ok(list));
            }
            catch (System.Exception)
            {
                throw new HttpResponseException
                {
                    Errors = new Onion.Common.Models.Error[] { _globalizationService.GetErrorInCurrentLanguage(ErrorCodes.UnknownException) },
                    StatusCode = System.Net.HttpStatusCode.InternalServerError
                };
            }
        }

        [HttpPost("stock/add")]
        public async Task<IActionResult> AddStock([FromBody] MovementRequestDto req)
        {
            var companyIdClaim = User?.FindFirst("CompanyId")?.Value;
            var performedBy = User?.Identity?.Name ?? "system";
            if (!int.TryParse(companyIdClaim, out var companyId))
                return BadRequest(Onion.Common.Models.ApiResponse<string>.Fail("CompanyId claim missing or invalid"));

            await _warehouseService.AddStockAsync(req, performedBy);
            return Ok(Onion.Common.Models.ApiResponse<object>.Ok(null, "Stock added"));
        }

        [HttpPost("stock/remove")]
        public async Task<IActionResult> RemoveStock([FromBody] MovementRequestDto req)
        {
            var companyIdClaim = User?.FindFirst("CompanyId")?.Value;
            var performedBy = User?.Identity?.Name ?? "system";
            if (!int.TryParse(companyIdClaim, out var companyId))
                return BadRequest(Onion.Common.Models.ApiResponse<string>.Fail("CompanyId claim missing or invalid"));

            await _warehouseService.RemoveStockAsync(req, performedBy);
            return Ok(Onion.Common.Models.ApiResponse<object>.Ok(null, "Stock removed"));
        }

        [HttpPost("stock/transfer")]
        public async Task<IActionResult> TransferStock([FromBody] TransferRequestDto req)
        {
            var companyIdClaim = User?.FindFirst("CompanyId")?.Value;
            var performedBy = User?.Identity?.Name ?? "system";
            if (!int.TryParse(companyIdClaim, out var companyId))
                return BadRequest(Onion.Common.Models.ApiResponse<string>.Fail("CompanyId claim missing or invalid"));

            await _warehouseService.TransferStockAsync(req, performedBy);
            return Ok(Onion.Common.Models.ApiResponse<object>.Ok(null, "Stock transferred"));
        }
    }
}
