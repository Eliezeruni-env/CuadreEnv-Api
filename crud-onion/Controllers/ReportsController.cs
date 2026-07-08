using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Services;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly IGlobalizationService _globalization;

        public ReportsController(IReportService reportService, IGlobalizationService globalization)
        {
            _reportService = reportService;
            _globalization = globalization;
        }

        [HttpGet("sales")]
        public async Task<IActionResult> Sales([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string? period = "day")
        {
            var res = await _reportService.GetSalesByPeriodAsync(from, to, period);
            return Ok(res);
        }

        [HttpGet("top-products")]
        public async Task<IActionResult> TopProducts([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var res = await _reportService.GetTopProductsAsync(from, to);
            return Ok(res);
        }

        [HttpGet("inventory-status")]
        public async Task<IActionResult> InventoryStatus()
        {
            var res = await _reportService.GetInventoryStatusAsync();
            return Ok(res);
        }

        [HttpGet("active-customers")]
        public async Task<IActionResult> ActiveCustomers([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var res = await _reportService.GetActiveCustomersAsync(from, to);
            return Ok(res);
        }

        [HttpGet("accounts-receivable-summary")]
        public async Task<IActionResult> AccountsReceivableSummary([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var res = await _reportService.GetAccountsReceivableSummaryAsync(from, to);
            return Ok(res);
        }
    }
}
