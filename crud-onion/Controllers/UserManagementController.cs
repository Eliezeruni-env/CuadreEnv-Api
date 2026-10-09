using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Dtos;
using Onion.Common.Models;

namespace Onion.Controllers
{
    // RoutePrefixConvention in Program.cs already prepends 'v1' to routes.
    // Use 'users' here so final route becomes '/v1/users'.
    [Route("users")]
    [ApiController]
    public class UserManagementController : ControllerBase
    {
    private readonly IUserManagementService _svc;
    private readonly Onion.Common.Services.ICurrentUserService _currentUserService;

        public UserManagementController(IUserManagementService svc, Onion.Common.Services.ICurrentUserService currentUserService)
        {
            _svc = svc;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? q = null, [FromQuery] string? role = null, [FromQuery] bool? active = null, [FromQuery] int? companyId = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var result = await _svc.GetPagedAsync(page, pageSize, q, role, active, companyId);
            return Ok(new { items = result.Items, total = result.Total, page, pageSize, totalPages = result.TotalPages });
        }

        [HttpGet("exists")]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> Exists([FromQuery] string? email = null, [FromQuery] string? username = null, [FromQuery] int? excludeId = null)
        {
            var exists = (!string.IsNullOrWhiteSpace(email) && await _svc.ExistsEmailAsync(email, excludeId)) ||
                         (!string.IsNullOrWhiteSpace(username) && await _svc.ExistsUserNameAsync(username, excludeId));
            return Ok(new { exists });
        }

        [HttpGet("{id:int}")]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> Get(int id)
        {
            var dto = await _svc.GetByIdAsync(id);
            if (dto == null) return NotFound(ApiResponse<string>.Fail("User not found"));
            return Ok(ApiResponse<UserDetailDto>.Ok(dto));
        }

        [HttpPost]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> Post([FromBody] CreateUserRequest req)
        {
            var performedBy = _currentUserService.UserId?.ToString() ?? "system";
            try
            {
                var created = await _svc.CreateAsync(req, performedBy, HttpContext.RequestAborted);
                var payload = new { id = created.Id, email = created.Email, temporaryPassword = created.TemporaryPassword, tempPasswordSent = created.TempPasswordSent };
                return CreatedAtAction(nameof(Get), new { id = created.Id }, ApiResponse<object>.Ok(payload, "User created"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpPut("{id:int}")]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateUserRequest req)
        {
            var performedBy = _currentUserService.UserId?.ToString() ?? "system";
            try
            {
                await _svc.UpdateAsync(id, req, performedBy);
                return Ok(ApiResponse<object>.Ok(null, "User updated"));
            }
            catch (InvalidOperationException ex)
            {
                if (string.Equals(ex.Message, "User not found", StringComparison.Ordinal))
                    return NotFound(ApiResponse<object>.Fail("User not found"));
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpPatch("{id:int}/status")]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> ToggleStatus(int id, [FromBody] Onion.BussinesLogic.Dtos.UserStatusToggleRequest req)
        {
            var performedBy = _currentUserService.UserId?.ToString() ?? "system";
            try
            {
                await _svc.ToggleActiveAsync(id, req.Active, performedBy);
                return Ok(ApiResponse<object>.Ok(null, "User status updated"));
            }
            catch (InvalidOperationException ex)
            {
                if (string.Equals(ex.Message, "User not found", StringComparison.Ordinal))
                    return NotFound(ApiResponse<object>.Fail("User not found"));
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpPost("{id:int}/reset-password")]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest req)
        {
            var performedBy = _currentUserService.UserId?.ToString() ?? "system";
            try
            {
                var result = await _svc.ResetPasswordAsync(id, req, performedBy);
                return Ok(ApiResponse<object>.Ok(new { result }, "Password reset"));
            }
            catch (InvalidOperationException ex)
            {
                if (string.Equals(ex.Message, "User not found", StringComparison.Ordinal))
                    return NotFound(ApiResponse<object>.Fail("User not found"));
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpDelete("{id:int}")]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var performedBy = _currentUserService.UserId?.ToString() ?? "system";
            try
            {
                await _svc.DeleteAsync(id, performedBy);
                return Ok(ApiResponse<object>.Ok(null, "User deleted"));
            }
            catch (InvalidOperationException ex)
            {
                if (string.Equals(ex.Message, "User not found", StringComparison.Ordinal))
                    return NotFound(ApiResponse<object>.Fail("User not found"));
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }
    }
}
