using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Onion.DataAccess;
using Onion.Domain.Users;

namespace crud_onion.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public sealed class MeController(OnionDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return Unauthorized();

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            companyId = user.CompanyId,
            role = user.Role,
            isSuperUser = user.IsSuperUser,
            access = true
        });
    }
}
