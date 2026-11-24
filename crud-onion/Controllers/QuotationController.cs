using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain.Quotations;

namespace Onion.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [Route("quotation")]
    [ApiController]
    public class QuotationController : ControllerBase
    {
        private readonly IQuotationService _quotationService;
        private readonly ILogger<QuotationController> _logger;

        public QuotationController(
            IQuotationService quotationService,
            ILogger<QuotationController> logger)
        {
            _quotationService = quotationService;
            _logger = logger;
        }

        // GET /api/quotation?status=aprobadas&search=cliente&startDate=...&endDate=...&page=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetQuotations(
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _quotationService.GetPagedQuotationsAsync(
                    status,
                    search,
                    startDate,
                    endDate,
                    page,
                    pageSize,
                    cancellationToken);

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching quotations");
                return StatusCode(500, new { success = false, message = "Error al obtener las cotizaciones.", detail = ex.Message });
            }
        }

        // GET /api/quotation/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var quotation = await _quotationService.GetByIdAsync(id, cancellationToken);
                if (quotation == null)
                {
                    return NotFound(new { success = false, message = $"Cotización #{id} no encontrada." });
                }

                return Ok(new { success = true, data = quotation });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching quotation {Id}", id);
                return StatusCode(500, new { success = false, message = "Error al obtener la cotización.", detail = ex.Message });
            }
        }

        // POST /api/quotation
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateQuotationDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { success = false, message = "Datos de cotización inválidos." });
                }

                var created = await _quotationService.CreateAsync(dto, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, new { success = true, message = "Cotización creada exitosamente.", data = created });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quotation");
                return StatusCode(500, new { success = false, message = "Error al crear la cotización.", detail = ex.Message });
            }
        }

        // PUT /api/quotation/{id}/status
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateQuotationStatusDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _quotationService.UpdateStatusAsync(id, dto.Status, cancellationToken);
                return Ok(new { success = true, message = $"Estado de cotización actualizado a {updated.StatusName}.", data = updated });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = $"Cotización #{id} no encontrada." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quotation status {Id}", id);
                return StatusCode(500, new { success = false, message = "Error al actualizar el estado de la cotización.", detail = ex.Message });
            }
        }

        // POST /api/quotation/{id}/convert-to-sale
        [HttpPost("{id:int}/convert-to-sale")]
        public async Task<IActionResult> ConvertToSale(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var sale = await _quotationService.ConvertToSaleAsync(id, cancellationToken);
                return Ok(new
                {
                    success = true,
                    message = $"Cotización convertida en Venta #{sale.Id} exitosamente.",
                     data = new { saleId = sale.Id, saleCode = sale.InvoiceFolio, total = sale.Total }
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = $"Cotización #{id} no encontrada." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting quotation {Id} to sale", id);
                return StatusCode(500, new { success = false, message = "Error al convertir la cotización a venta.", detail = ex.Message });
            }
        }

        // DELETE /api/quotation/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var deleted = await _quotationService.DeleteAsync(id, cancellationToken);
                if (!deleted)
                {
                    return NotFound(new { success = false, message = $"Cotización #{id} no encontrada." });
                }

                return Ok(new { success = true, message = "Cotización eliminada correctamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quotation {Id}", id);
                return StatusCode(500, new { success = false, message = "Error al eliminar la cotización.", detail = ex.Message });
            }
        }
    }
}
