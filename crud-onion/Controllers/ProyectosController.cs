using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Onion.DataAccess;
using Onion.DataAccess.Clerk;

namespace crud_onion.Controllers;

[ApiController]
[Authorize]
[Route("api/proyectos")]
public sealed class ProyectosController(OnionDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await db.Proyectos.AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = "LocalCompanyAdmin")]
    public async Task<IActionResult> Create(CreateProyectoRequest request, CancellationToken cancellationToken)
    {
        var companyIdClaim = User.FindFirst("companyId")?.Value
            ?? User.FindFirst("CompanyId")?.Value;
        if (!int.TryParse(companyIdClaim, out var companyId) || companyId <= 0) return Forbid();
        var companyExists = await db.Companies.AnyAsync(x => x.Id == companyId, cancellationToken);
        if (!companyExists) return Conflict(new { message = "La empresa no existe." });

        var proyecto = new Proyecto
        {
            // Keep the existing column during the non-invasive transition.
            OrganizationId = companyId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion,
            CreatedAt = DateTime.UtcNow
        };
        db.Proyectos.Add(proyecto);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = proyecto.Id }, proyecto);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken cancellationToken)
    {
        var proyecto = await db.Proyectos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return proyecto is null ? NotFound() : Ok(proyecto);
    }

    public sealed record CreateProyectoRequest(string Nombre, string? Descripcion);
}
