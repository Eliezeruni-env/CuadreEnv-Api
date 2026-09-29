using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Dtos;

namespace CrudOnion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CreditsController : ControllerBase
    {
        private readonly ICreditService _svc;

        public CreditsController(ICreditService svc)
        {
            _svc = svc;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int? pageNumber = null, [FromQuery] int? pageSize = null)
        {
            if (pageNumber.HasValue)
            {
                var paged = await _svc.ListPagedAsync(pageNumber.Value, pageSize ?? 10);
                return Ok(paged);
            }

            var list = (await _svc.ListAsync()).ToList();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var e = await _svc.GetByIdAsync(id);
            if (e == null) return NotFound();
            return Ok(e);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCreditDto dto)
        {
            var created = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPatch("{id}/payments")]
        public async Task<IActionResult> AddPayment(int id, [FromBody] CreateCreditPaymentDto dto)
        {
            try
            {
                var p = await _svc.AddPaymentAsync(id, dto);
                return Ok(p);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            // simple endpoint to allow manual status update if required
            var existing = await _svc.GetByIdAsync(id);
            if (existing == null) return NotFound();
            // Not implementing full manual status change to avoid breaking business rules
            return BadRequest(new { message = "Manual status changes are restricted. Use scheduled processes or payments to change status." });
        }
    }
}
