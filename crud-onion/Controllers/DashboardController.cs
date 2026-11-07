using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace crud_onion.Controllers;

[ApiController]
[Route("api")]
[Route("dashboard")]
[Route("[controller]")]
public sealed class DashboardController : ControllerBase
{
    [HttpGet("data")]
    [Authorize]
    public IActionResult GetData()
    {
        return Ok(new
        {
            message = "Acceso autorizado al dashboard local.",
            userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value,
            companyId = User.FindFirst("companyId")?.Value
                ?? User.FindFirst("CompanyId")?.Value
        });
    }
}
