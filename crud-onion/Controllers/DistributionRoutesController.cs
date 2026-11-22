using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Authorization;

namespace Onion.Controllers
{
    [Authorize]
    [AuthorizeModule("sales")]
    [Route("api/[controller]")]
    [Route("[controller]")]
    [ApiController]
    public class DistributionRoutesController : ControllerBase
    {
        private readonly IDistributionRouteService _service;
        private readonly ILogger<DistributionRoutesController> _logger;

        public DistributionRoutesController(
            IDistributionRouteService service,
            ILogger<DistributionRoutesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET /api/DistributionRoutes
        [HttpGet]
        public async Task<IActionResult> GetRoutes(
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? date = null,
            CancellationToken ct = default)
        {
            try
            {
                var routes = await _service.GetRoutesAsync(status, search, date, ct);
                return Ok(new { success = true, data = routes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching distribution routes");
                return StatusCode(500, new { success = false, message = "Error al obtener las rutas de distribución.", detail = ex.Message });
            }
        }

        // GET /api/DistributionRoutes/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetRouteById(int id, CancellationToken ct = default)
        {
            try
            {
                var route = await _service.GetRouteByIdAsync(id, ct);
                if (route == null) return NotFound(new { success = false, message = $"Ruta {id} no encontrada." });
                return Ok(new { success = true, data = route });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching distribution route {Id}", id);
                return StatusCode(500, new { success = false, message = "Error al obtener la ruta.", detail = ex.Message });
            }
        }

        // POST /api/DistributionRoutes
        [HttpPost]
        public async Task<IActionResult> CreateRoute([FromBody] CreateDistributionRouteRequest request, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RouteName))
                    return BadRequest(new { success = false, message = "El nombre de la ruta es requerido." });

                var created = await _service.CreateRouteAsync(request, ct);
                return CreatedAtAction(nameof(GetRouteById), new { id = created.Id }, new { success = true, data = created });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating distribution route");
                return StatusCode(500, new { success = false, message = "Error al crear la hoja de despacho.", detail = ex.Message });
            }
        }

        // POST /api/DistributionRoutes/{id}/load-items
        [HttpPost("{id:int}/load-items")]
        public async Task<IActionResult> LoadItems(int id, [FromBody] LoadRouteItemsRequest request, CancellationToken ct = default)
        {
            try
            {
                var updated = await _service.LoadItemsAsync(id, request, ct);
                return Ok(new { success = true, data = updated, message = "Mercancía cargada al camión exitosamente." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading items to route {Id}", id);
                return StatusCode(500, new { success = false, message = "Error al cargar mercancía.", detail = ex.Message });
            }
        }

        // POST /api/DistributionRoutes/{id}/sale
        [HttpPost("{id:int}/sale")]
        public async Task<IActionResult> RegisterSale(int id, [FromBody] MobileRouteSaleRequest request, CancellationToken ct = default)
        {
            try
            {
                var updated = await _service.RegisterMobileSaleAsync(id, request, ct);
                return Ok(new { success = true, data = updated, message = "Venta en ruta registrada exitosamente." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering sale on route {Id}", id);
                return StatusCode(500, new { success = false, message = "Error al registrar la venta en ruta.", detail = ex.Message });
            }
        }

        // POST /api/DistributionRoutes/{id}/liquidate
        [HttpPost("{id:int}/liquidate")]
        public async Task<IActionResult> Liquidate(int id, [FromBody] LiquidateRouteRequest request, CancellationToken ct = default)
        {
            try
            {
                var liquidated = await _service.LiquidateRouteAsync(id, request, ct);
                return Ok(new { success = true, data = liquidated, message = "Ruta liquidada exitosamente." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liquidating route {Id}", id);
                return StatusCode(500, new { success = false, message = "Error al liquidar la ruta.", detail = ex.Message });
            }
        }
    }
}
