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
    public class BankReconciliationController : ControllerBase
    {
        private readonly IBankReconciliationService _service;
        private readonly ILogger<BankReconciliationController> _logger;

        public BankReconciliationController(
            IBankReconciliationService service,
            ILogger<BankReconciliationController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET /api/BankReconciliation/records
        [HttpGet("records")]
        public async Task<IActionResult> GetRecords(
            [FromQuery] string? bank = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            try
            {
                var records = await _service.GetRecordsAsync(bank, status, fromDate, toDate, search, ct);
                return Ok(new { success = true, data = records });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bank reconciliation records");
                return StatusCode(500, new { success = false, message = "Error al obtener las transferencias bancarias.", detail = ex.Message });
            }
        }

        // GET /api/BankReconciliation/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(CancellationToken ct = default)
        {
            try
            {
                var stats = await _service.GetStatsAsync(ct);
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bank reconciliation stats");
                return StatusCode(500, new { success = false, message = "Error al calcular estadísticas de conciliación.", detail = ex.Message });
            }
        }

        // POST /api/BankReconciliation/{id}/reconcile
        [HttpPost("{id:int}/reconcile")]
        public async Task<IActionResult> Reconcile(int id, [FromBody] ReconcileTransferRequest request, CancellationToken ct = default)
        {
            try
            {
                var reconciled = await _service.ReconcileAsync(id, request, ct);
                return Ok(new { success = true, data = reconciled, message = "Transferencia conciliada exitosamente." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reconciling transfer {Id}", id);
                return StatusCode(500, new { success = false, message = "Error al conciliar la transferencia.", detail = ex.Message });
            }
        }

        // POST /api/BankReconciliation/{id}/discrepancy
        [HttpPost("{id:int}/discrepancy")]
        public async Task<IActionResult> MarkDiscrepancy(int id, [FromBody] MarkDiscrepancyRequest request, CancellationToken ct = default)
        {
            try
            {
                var discrepant = await _service.MarkDiscrepancyAsync(id, request, ct);
                return Ok(new { success = true, data = discrepant, message = "Discrepancia registrada correctamente." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking discrepancy on transfer {Id}", id);
                return StatusCode(500, new { success = false, message = "Error al registrar discrepancia.", detail = ex.Message });
            }
        }

        // POST /api/BankReconciliation/import-statement
        [HttpPost("import-statement")]
        public async Task<IActionResult> ImportStatement([FromBody] ImportStatementRequest request, CancellationToken ct = default)
        {
            try
            {
                var matchedCount = await _service.ImportStatementAsync(request, ct);
                return Ok(new { success = true, matchedCount, message = $"Se procesaron y cotejaron {matchedCount} transacciones del extracto." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing bank statement");
                return StatusCode(500, new { success = false, message = "Error al importar el extracto bancario.", detail = ex.Message });
            }
        }
    }
}
