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

        private int GetCurrentCompanyId()
        {
            var claim = User.FindFirst("CompanyId") ?? User.FindFirst("companyId") ?? User.FindFirst("tenantId");
            return claim != null ? int.Parse(claim.Value) : 1;
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var companyId = GetCurrentCompanyId();
            var dto = await _svc.GetSettingsAsync(companyId);
            return Ok(dto);
        }

        [HttpPut("settings")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateCompanySettingsDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var companyId = GetCurrentCompanyId();
            await _svc.UpdateSettingsAsync(companyId, request);
            return Ok(new { success = true, message = "Configuración institucional guardada exitosamente." });
        }
    }
}
