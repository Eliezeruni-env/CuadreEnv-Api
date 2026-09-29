using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;

namespace Onion.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    public class PasswordController : ControllerBase
    {
        private readonly IUserManagementService _userSvc;
        private readonly Onion.Common.Services.ICurrentUserService _currentUser;

        public PasswordController(IUserManagementService userSvc, Onion.Common.Services.ICurrentUserService currentUser)
        {
            _userSvc = userSvc;
            _currentUser = currentUser;
        }

        // Self-change password
        [HttpPost("change")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            var userId = _currentUser.UserId;
            if (!userId.HasValue) return Unauthorized();

            // For now use ResetPasswordAsync to set password (admin-like). In a real impl validate current password.
            var resetReq = new ResetPasswordRequest { SendByEmail = false, TemporaryPassword = req.NewPassword };
            await _userSvc.ResetPasswordAsync(userId.Value, resetReq, userId.Value.ToString());
            return Ok(Onion.Common.Models.ApiResponse<object>.Ok(null, "Password changed"));
        }
    }
}
