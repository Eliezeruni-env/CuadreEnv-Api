using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using Onion.Common.Enums;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _service;
        private readonly IUserService _userService;
        private readonly IGlobalizationService _globalizationService;

        public CompanyController(ICompanyService service, IUserService userService, IGlobalizationService globalizationService)
        {
            _service = service;
            _userService = userService;
            _globalizationService = globalizationService;
        }

        // NOTE: This endpoint exposes all companies in the system. It must be restricted to platform administrators
        // (Module 3) before being enabled. For now, require platform admin role and return 501 to ensure it is not accidentally used.
        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public Task<IActionResult> GetAll()
        {
            // TODO(MODULE-3): Implement platform-admin-only listing of companies. Endpoint intentionally disabled until then.
            return Task.FromResult<IActionResult>(StatusCode(501, Onion.Common.Models.ApiResponse<object>.Fail("Endpoint disabled until platform admin roles are implemented")));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            // Allow access if the caller is the owner of the company (CompanyId claim matches)
            // or if the caller has the SuperAdmin platform role. Otherwise forbid access.
            var companyIdClaim = User?.FindFirst("CompanyId")?.Value;
            var isSuperAdmin = User?.IsInRole("SuperAdmin") ?? false;

            if (!isSuperAdmin)
            {
                if (!int.TryParse(companyIdClaim, out var callerCompanyId) || callerCompanyId != id)
                {
                    // Do not reveal existence of other companies — forbid access for non-admins.
                    return Forbid();
                }
            }

            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(Onion.Common.Models.ApiResponse<object>.Fail("Company not found"));
            return Ok(Onion.Common.Models.ApiResponse<object>.Ok(item));
        }

        // Create company and assign the creating user as owner
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Onion.BussinesLogic.Dtos.CreateCompanyRequest request)
        {
            var created = await _service.CreateAsync(request);

            // If user is authenticated, associate user to the created company
            var userIdClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                await _userService.AssignCompanyAsync(userId, created.Id);
            }

            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] Company company)
        {
            await _service.UpdateAsync(company);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
