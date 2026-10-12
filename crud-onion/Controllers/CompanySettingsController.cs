using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;

namespace Onion.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
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
            var claim = User.FindFirst("CompanyId") ?? User.FindFirst("companyId") ?? User.FindFirst("tenantId");
            return int.TryParse(claim?.Value, out companyId) && companyId > 0;
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            if (!TryGetCurrentCompanyId(out var companyId)) return Unauthorized();
            var dto = await _svc.GetSettingsAsync(companyId);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPut("settings")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateCompanySettingsDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!TryGetCurrentCompanyId(out var companyId)) return Unauthorized();
            await _svc.UpdateSettingsAsync(companyId, request);
            return Ok(new { success = true, message = "Configuración institucional guardada exitosamente." });
        }
    }
}
