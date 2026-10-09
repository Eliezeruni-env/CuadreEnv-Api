using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Dtos;
using System.Threading.Tasks;
using System.Collections.Generic;
using Onion.Common.Features;

namespace Onion.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly Onion.BussinesLogic.Services.Abstract.IAppointmentService _svc;
        private readonly Onion.Common.Features.IFeatureService _feature;

        public AppointmentsController(Onion.BussinesLogic.Services.Abstract.IAppointmentService svc, Onion.Common.Features.IFeatureService feature)
        {
            _svc = svc;
            _feature = feature;
        }

        private async Task<bool> EnsureFeatureAsync()
        {
            // Use feature key 'appointments' to gate functionality
            var companyIdClaim = HttpContext.User.FindFirst("companyId")?.Value
                ?? HttpContext.User.FindFirst("CompanyId")?.Value;
            int companyId = 0;
            if (int.TryParse(companyIdClaim, out var cid)) companyId = cid;
            // If no company context, allow in development scenarios by returning true
            if (companyId == 0) return true;
            return await _feature.CompanyHasFeatureAsync(companyId, "appointments");
        }

        [HttpGet]
        public async Task<IEnumerable<AppointmentDto>> List()
        {
            if (!await EnsureFeatureAsync()) return new List<AppointmentDto>();
            return await _svc.ListAsync();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            if (!await EnsureFeatureAsync()) return NotFound();
            var e = await _svc.GetByIdAsync(id);
            if (e == null) return NotFound();
            return Ok(e);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
        {
            if (!await EnsureFeatureAsync()) return NotFound();
            try
            {
                var created = await _svc.CreateAsync(dto);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAppointmentDto dto)
        {
            if (!await EnsureFeatureAsync()) return NotFound();
            if (id != dto.Id) return BadRequest();
            try
            {
                var up = await _svc.UpdateAsync(dto);
                return Ok(up);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await EnsureFeatureAsync()) return NotFound();
            await _svc.DeleteAsync(id);
            return NoContent();
        }
    }
}
