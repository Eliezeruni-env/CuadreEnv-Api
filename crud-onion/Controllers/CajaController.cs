using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Dtos;
using Onion.Common.Authorization;

namespace Onion.Controllers
{
    [Route("caja/sales")]
    [ApiController]
    [AuthorizeModule("POS")]
    public class CajaController : ControllerBase
    {
        private readonly ICajaService _service;
        private readonly Onion.BussinesLogic.Services.Abstract.ISaleService _saleService;

        public CajaController(ICajaService service, Onion.BussinesLogic.Services.Abstract.ISaleService saleService)
        {
            _service = service;
            _saleService = saleService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaleDto dto)
        {
            if (dto == null) return BadRequest(Onion.Common.Models.ApiResponse<object>.Fail("Body required"));

            var key = dto.IdempotencyKey;
            if (string.IsNullOrWhiteSpace(key) && Request.Headers.TryGetValue("X-Idempotency-Key", out var headerKey))
            {
                key = headerKey.ToString();
            }

            if (string.IsNullOrWhiteSpace(key)) return BadRequest(Onion.Common.Models.ApiResponse<object>.Fail("IdempotencyKey required"));

            var sale = new Onion.Domain.Sale
            {
                CustomerId = dto.CustomerId,
                Date = dto.Date,
                Notes = dto.Notes,
                CreateBy = dto.CreateBy
            };
            foreach (var it in dto.Items)
            {
                sale.Details.Add(new Onion.Domain.SaleDetail { ProductId = it.ProductId, Quantity = it.Quantity, UnitPrice = it.UnitPrice });
            }

            var created = await _service.CreateSaleAsync(sale, key, dto.CashRegisterId);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, Onion.Common.Models.ApiResponse<object>.Ok(created));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var sale = await _saleService.GetByIdAsync(id);
            if (sale == null) return NotFound(Onion.Common.Models.ApiResponse<object>.Fail("Sale not found"));
            return Ok(Onion.Common.Models.ApiResponse<object>.Ok(sale));
        }
    }
}
