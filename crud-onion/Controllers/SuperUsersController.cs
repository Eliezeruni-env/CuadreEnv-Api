using Microsoft.AspNetCore.Mvc;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain.Users;
using Onion.Common.Models;

namespace Onion.Controllers
{
    [Route("users/superusers")]
    [ApiController]
    public class SuperUsersController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly Onion.DataAccess.OnionDbContext _db;

        public SuperUsersController(IUnitOfWork uow, Onion.DataAccess.OnionDbContext db)
        {
            _uow = uow;
            _db = db;
        }

        // GET /v1/users/superusers
        [HttpGet]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> GetAll()
        {
            var all = (await _uow.Users.ListAsync()).ToList();
            var su = all.Where(u => u.IsSuperUser).Select(u => new { u.Id, u.Email, u.FirstName, u.LastName }).ToList();
            return Ok(ApiResponse<object>.Ok(su));
        }

        public record SetSuperUserRequest(int UserId);

        // POST /v1/users/superusers -> body { userId }
        [HttpPost]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> Set([FromBody] SetSuperUserRequest req)
        {
            var user = await _uow.Users.GetByIdAsync(req.UserId);
            if (user == null) return NotFound(ApiResponse<string>.Fail("User not found"));
            user.IsSuperUser = true;
            _uow.Users.Update(user);
            // create audit log
            var audit = new Onion.Domain.Audit.AuditLog
            {
                Action = "SetSuperUser",
                Entity = "User",
                EntityId = user.Id,
                PerformedBy = User?.FindFirst("sub")?.Value ?? "system",
                Details = $"Marked user {user.Email} as super-user"
            };
            await _db.Set<Onion.Domain.Audit.AuditLog>().AddAsync(audit);
            await _uow.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(new { user.Id, user.Email }, "Super-user set"));
        }

        // DELETE /v1/users/superusers/{id}
        [HttpDelete("{userId}")]
        [Onion.Common.Authorization.RequireRole("Admin")]
        public async Task<IActionResult> Unset(int userId)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) return NotFound(ApiResponse<string>.Fail("User not found"));
            user.IsSuperUser = false;
            _uow.Users.Update(user);
            var audit = new Onion.Domain.Audit.AuditLog
            {
                Action = "UnsetSuperUser",
                Entity = "User",
                EntityId = user.Id,
                PerformedBy = User?.FindFirst("sub")?.Value ?? "system",
                Details = $"Unmarked user {user.Email} as super-user"
            };
            await _db.Set<Onion.Domain.Audit.AuditLog>().AddAsync(audit);
            await _uow.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(new { user.Id, user.Email }, "Super-user removed"));
        }
    }
}
