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
        private readonly Onion.BussinesLogic.Services.Abstract.IAuthService _authService;
        private readonly AutoMapper.IMapper _mapper;

        public CompanyController(ICompanyService service, IUserService userService, IGlobalizationService globalizationService, Onion.BussinesLogic.Services.Abstract.IAuthService authService, AutoMapper.IMapper mapper)
        {
            _service = service;
            _userService = userService;
            _globalizationService = globalizationService;
            _authService = authService;
            _mapper = mapper;
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
        [Onion.Common.Authorization.RequirePermission(new[] { Onion.Common.Authorization.RolesConstants.SuperAdmin }, null, "id")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(Onion.Common.Models.ApiResponse<object>.Fail("Company not found"));

            var dto = _mapper.Map<Onion.BussinesLogic.Dtos.CompanyDto>(item);
            return Ok(Onion.Common.Models.ApiResponse<object>.Ok(dto));
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
                // Issue new access/refresh tokens that include CompanyId claim for the assigned user
                try
                {
                    var tokens = await _authService.IssueTokensForUserAsync(userId);
                    return CreatedAtAction(nameof(Get), new { id = created.Id }, Onion.Common.Models.ApiResponse<object>.Ok(new { company = created, tokens }, "Company created; tokens issued"));
                }
                catch
                {
                    // If token issuance fails, still return created company without tokens
                }
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
