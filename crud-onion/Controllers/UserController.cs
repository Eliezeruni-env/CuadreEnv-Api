using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain.Users;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using Onion.Common.Enums;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _service;
        private readonly IGlobalizationService _globalizationService;
        private readonly Onion.Common.Services.ICurrentUserService _currentUserService;

        public UserController(IUserService service, IGlobalizationService globalizationService, Onion.Common.Services.ICurrentUserService currentUserService)
        {
            _service = service;
            _globalizationService = globalizationService;
            _currentUserService = currentUserService;
        }

        public record ChangeRoleRequest(string Role);

        [HttpGet("cashiers")]
        public async Task<IActionResult> GetCashiers()
        {
            var users = await _service.GetCashiersAsync();
            return Ok(users.Select(u => new { u.Id, u.FirstName, u.LastName, u.Email, u.UserName, u.Role }));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var items = await _service.GetAllAsync();
                return Ok(items);
            }
            catch (System.Exception)
            {
                throw new HttpResponseException
                {
                    Errors = new Onion.Common.Models.Error[] { _globalizationService.GetErrorInCurrentLanguage(ErrorCodes.UnknownException) },
                    StatusCode = System.Net.HttpStatusCode.InternalServerError
                };
            }
        }

        // Change role (Admin only)
        [HttpPut("{id}/role")]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Role))
                return BadRequest(Onion.Common.Models.ApiResponse<string>.Fail("Role required"));

            await _service.SetRoleAsync(id, req.Role);
            return NoContent();
        }

        [HttpPost("{id}/deactivate")]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> Deactivate(int id)
        {
            await _service.SetActiveAsync(id, false);
            return NoContent();
        }

        [HttpPost("{id}/reactivate")]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> Reactivate(int id)
        {
            await _service.SetActiveAsync(id, true);
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            item.PasswordHash = string.Empty;
            return Ok(item);
        }

        [HttpPost]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> Post([FromBody] User user)
        {
            var created = await _service.CreateAsync(user);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, new { created.Id, created.Email });
        }

        // Register employee for current company (Admins)
        [HttpPost("register-employee")]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> RegisterEmployee([FromBody] User user)
        {
            // Ensure employee assigned to current tenant
            var companyId = _currentUserService.CompanyId ?? throw new System.Exception("Company context missing");
            user.CompanyId = companyId;
            var created = await _service.CreateAsync(user);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, Onion.Common.Models.ApiResponse<object>.Ok(new { created.Id, created.Email }, "Employee created"));
        }

        [HttpPut]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> Put([FromBody] User user)
        {
            await _service.UpdateAsync(user);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
