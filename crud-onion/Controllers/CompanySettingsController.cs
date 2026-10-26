using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Authorization;

namespace Onion.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class CompanySettingsController : ControllerBase
    {
        private readonly ICompanySettingsService _svc;

        public CompanySettingsController(ICompanySettingsService svc)
        {
            _svc = svc;
        }

        private bool TryGetCurrentCompanyId(out int companyId)
        {
            var claim = User.FindFirst("companyId") ?? User.FindFirst("CompanyId") ?? User.FindFirst("tenantId") ?? User.FindFirst("TenantId");
            if (int.TryParse(claim?.Value, out companyId) && companyId > 0)
                return true;

            if (Request.Headers.TryGetValue("X-Company-Id", out var headerVal) && int.TryParse(headerVal.FirstOrDefault(), out companyId) && companyId > 0)
                return true;

            return false;
        }

        [HttpGet]
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            if (!TryGetCurrentCompanyId(out var companyId)) return Unauthorized();
            var dto = await _svc.GetSettingsAsync(companyId);
            return Ok(dto ?? new CompanySettingsEditableDto { Currency = "DOP", DefaultTaxPercentage = 18 });
        }

        [HttpPut]
        [HttpPut("settings")]
        [AuthorizeAnyModule("company-settings", "sales", "cashregister", "pos")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateCompanySettingsDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!TryGetCurrentCompanyId(out var companyId)) return Unauthorized();
            await _svc.UpdateSettingsAsync(companyId, request);
            return Ok(new { success = true, message = "Configuración institucional guardada exitosamente." });
        }
    }
}
