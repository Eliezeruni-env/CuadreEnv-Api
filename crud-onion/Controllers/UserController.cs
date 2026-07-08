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

        public UserController(IUserService service, IGlobalizationService globalizationService)
        {
            _service = service;
            _globalizationService = globalizationService;
        }

        public record ChangeRoleRequest(string Role);

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
        public async Task<IActionResult> Post([FromBody] User user)
        {
            var created = await _service.CreateAsync(user);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, new { created.Id, created.Email });
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] User user)
        {
            await _service.UpdateAsync(user);
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
