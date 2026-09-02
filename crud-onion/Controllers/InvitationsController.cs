using Microsoft.AspNetCore.Mvc;
using Onion.DataAccess.Repositories.Concrete;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain.Invitations;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using Onion.Common.Enums;
using Onion.Common.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class InvitationsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IGlobalizationService _globalization;
        private readonly Onion.Common.Services.IEmailService? _emailService;
        private readonly Onion.Common.Authorization.IAuthorizationService _auth;

        public InvitationsController(IUnitOfWork uow, IGlobalizationService globalization, Onion.Common.Services.IEmailService? emailService = null, Onion.Common.Authorization.IAuthorizationService? auth = null)
        {
            _uow = uow;
            _globalization = globalization;
            _emailService = emailService;
            _auth = auth ?? new Onion.Common.Authorization.AuthorizationService();
        }

        // Admins invite users to their company
        [HttpPost]
        [RequireRole("Admin")]
        public async Task<IActionResult> Create([FromBody] InvitationRequest req)
        {
            if (!_auth.TryGetCompanyId(User, out var companyId))
                return BadRequest(Onion.Common.Models.ApiResponse<string>.Fail("CompanyId claim missing or invalid"));

            var invitedByClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(invitedByClaim, out var invitedById);

            var token = Guid.NewGuid().ToString("N");
            var inv = new Invitation
            {
                Email = req.Email,
                CompanyId = companyId,
                InvitedByUserId = invitedById,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(req.ValidDays > 0 ? req.ValidDays : 7),
                Accepted = false
            };

            await _uow.Invitations.AddAsync(inv);
            await _uow.SaveChangesAsync();

            // Try to send invitation email if service configured; don't fail if email sending fails
            try
            {
                if (_emailService != null)
                {
                    await _emailService.SendInvitationAsync(req.Email, token, companyId);
                }
            }
            catch { }

            // Return token for testing; in production do not return token in response
            return Ok(new { token = token, expiresAt = inv.ExpiresAt });
        }

        // Accept invitation: creates user associated to the company
        [AllowAnonymous]
        [HttpPost("accept")]
        public async Task<IActionResult> Accept([FromBody] AcceptInvitationRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Token))
                return BadRequest(Onion.Common.Models.ApiResponse<string>.Fail("Token required"));

            var list = await _uow.Invitations.FindAsync(i => i.Token == req.Token);
            var inv = list.FirstOrDefault();
            if (inv == null) return NotFound(Onion.Common.Models.ApiResponse<string>.Fail("Invitation not found"));
            if (inv.Accepted) return BadRequest(Onion.Common.Models.ApiResponse<string>.Fail("Invitation already accepted"));
            if (inv.ExpiresAt < DateTime.UtcNow) return BadRequest(Onion.Common.Models.ApiResponse<string>.Fail("Invitation expired"));

            // Create user with provided password
            var user = new Onion.Domain.Users.User
            {
                Email = inv.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                FirstName = req.FirstName ?? string.Empty,
                LastName = req.LastName ?? string.Empty,
                UserName = string.IsNullOrWhiteSpace(req.UserName) ? inv.Email : req.UserName,
                CompanyId = inv.CompanyId,
                Role = req.Role ?? "Employee"
            };

            await _uow.Users.AddAsync(user);
            await _uow.SaveChangesAsync();

            inv.Accepted = true;
            inv.AcceptedByUserId = user.Id;
            inv.AcceptedAt = DateTime.UtcNow;
            _uow.Invitations.Update(inv);
            await _uow.SaveChangesAsync();

            return Ok(Onion.Common.Models.ApiResponse<object>.Ok(new { user.Id, user.Email }, "User created from invitation"));
        }
    }

    public record InvitationRequest(string Email, int ValidDays = 7);
    public record AcceptInvitationRequest(string Token, string Password, string? FirstName = null, string? LastName = null, string? UserName = null, string? Role = null);
}
