using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Onion.DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Onion.Controllers
{
    [Route("internal/migrations")]
    [ApiController]
    public class InternalMigrationsController : ControllerBase
    {
        private readonly OnionDbContext _db;
        private readonly ILogger<InternalMigrationsController> _logger;

        public InternalMigrationsController(OnionDbContext db, ILogger<InternalMigrationsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // POST /internal/migrations/apply
        // Protected by InternalApiAuthMiddleware (X-Internal-ApiKey or SuperAdmin role)
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyMigrations()
        {
            try
            {
                _logger.LogInformation("Applying pending EF Core migrations (requested via internal endpoint)");
                await _db.Database.MigrateAsync();
                _logger.LogInformation("Migrations applied successfully");
                return Ok(new { result = "ok", message = "Migrations applied" });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to apply migrations");
                return StatusCode(500, new { result = "error", message = "Failed to apply migrations", detail = ex.Message });
            }
        }

        // GET /internal/migrations/pending
        [HttpGet("pending")]
        public IActionResult GetPending()
        {
            try
            {
                var pending = _db.Database.GetPendingMigrations().ToList();
                return Ok(new { result = "ok", pending = pending });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate pending migrations");
                return StatusCode(500, new { result = "error", message = ex.Message });
            }
        }
    }
}
