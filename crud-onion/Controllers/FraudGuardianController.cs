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
    [AuthorizeModule("audit")]
    [Route("api/[controller]")]
    [Route("[controller]")]
    [ApiController]
    public class FraudGuardianController : ControllerBase
    {
        private readonly IFraudGuardianService _service;
        private readonly ILogger<FraudGuardianController> _logger;

        public FraudGuardianController(
            IFraudGuardianService service,
            ILogger<FraudGuardianController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET /api/FraudGuardian/alerts
        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts([FromQuery] string? tab = "all", CancellationToken ct = default)
        {
            try
            {
                var alerts = await _service.GetAlertsAsync(tab, ct);
                return Ok(new { success = true, data = alerts });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching fraud guardian alerts");
                return StatusCode(500, new { success = false, message = "Error al obtener las alertas forenses.", detail = ex.Message });
            }
        }

        // GET /api/FraudGuardian/cashier-scores
        [HttpGet("cashier-scores")]
        public async Task<IActionResult> GetCashierScores(CancellationToken ct = default)
        {
            try
            {
                var scores = await _service.GetCashierScoresAsync(ct);
                return Ok(new { success = true, data = scores });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cashier audit scores");
                return StatusCode(500, new { success = false, message = "Error al obtener la actividad de cajeros.", detail = ex.Message });
            }
        }

        // POST /api/FraudGuardian/alerts
        [HttpPost("alerts")]
        public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentRequest request, CancellationToken ct = default)
        {
            try
            {
                var created = await _service.CreateIncidentAsync(request, ct);
                return Ok(new { success = true, data = created, message = "Incidencia forense registrada." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fraud incident");
                return StatusCode(500, new { success = false, message = "Error al registrar la incidencia.", detail = ex.Message });
            }
        }

        // POST /api/FraudGuardian/alerts/{id}/resolve
        [HttpPost("alerts/{id}/resolve")]
        public async Task<IActionResult> ResolveIncident(string id, [FromBody] ResolveIncidentRequest request, CancellationToken ct = default)
        {
            try
            {
                var resolved = await _service.ResolveIncidentAsync(id, request, ct);
                return Ok(new { success = true, data = resolved, message = "Incidencia revisada y resuelta correctamente." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving incident {Id}", id);
                return StatusCode(500, new { success = false, message = "Error al resolver la incidencia.", detail = ex.Message });
            }
        }

        // POST /api/FraudGuardian/alerts/{id}/dismiss
        [HttpPost("alerts/{id}/dismiss")]
        public async Task<IActionResult> DismissIncident(string id, [FromBody] DismissIncidentRequest request, CancellationToken ct = default)
        {
            try
            {
                var dismissed = await _service.DismissIncidentAsync(id, request, ct);
                return Ok(new { success = true, data = dismissed, message = "Incidencia descartada como justificada." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dismissing incident {Id}", id);
                return StatusCode(500, new { success = false, message = "Error al descartar la incidencia.", detail = ex.Message });
            }
        }
    }
}
