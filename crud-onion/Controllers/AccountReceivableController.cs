using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain.Finance;
using Onion.Common.Services;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountReceivableController : ControllerBase
    {
        private readonly IAccountReceivableService _service;
        private readonly IGlobalizationService _globalization;

        public AccountReceivableController(IAccountReceivableService service, IGlobalizationService globalization)
        {
            _service = service;
            _globalization = globalization;
        }

        [HttpGet("overdue")]
        public async Task<IActionResult> GetOverdue()
        {
            var items = await _service.GetOverdueAsync();
            return Ok(items);
        }

        [HttpGet("due-soon")]
        public async Task<IActionResult> GetDueSoon([FromQuery] int days = 7)
        {
            var items = await _service.GetDueSoonAsync(days);
            return Ok(items);
        }

        [HttpPost("{id}/payments")]
        public async Task<IActionResult> RegisterPayment(int id, [FromBody] PaymentRequest req)
        {
            await _service.RegisterPaymentAsync(id, req.Amount);
            return NoContent();
        }
    }

    public record PaymentRequest(decimal Amount);
}
