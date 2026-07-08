using Microsoft.AspNetCore.Mvc;
using Onion.Common.Services;
using Onion.DataAccess.Repositories.Concrete;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CompanySettingsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public CompanySettingsController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var companyClaim = User?.FindFirst("CompanyId")?.Value;
            if (!int.TryParse(companyClaim, out var companyId)) return BadRequest("CompanyId claim missing");
            var settingsList = await _uow.CompanySettingsRepo.FindAsync(s => s.CompanyId == companyId);
            var settings = settingsList.FirstOrDefault();
            if (settings == null) return NotFound();
            return Ok(settings);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Onion.Domain.CompanySettings settings)
        {
            var companyClaim = User?.FindFirst("CompanyId")?.Value;
            if (!int.TryParse(companyClaim, out var companyId)) return BadRequest("CompanyId claim missing");
            if (settings.CompanyId != companyId) return Forbid();

            _uow.CompanySettingsRepo.Update(settings);
            await _uow.SaveChangesAsync();
            return NoContent();
        }
    }
}
