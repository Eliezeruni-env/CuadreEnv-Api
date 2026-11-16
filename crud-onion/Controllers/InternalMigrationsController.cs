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
                await _db.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE IF EXISTS ""Warehouses"" ADD COLUMN IF NOT EXISTS ""Code"" text NOT NULL DEFAULT '';
                    UPDATE ""Warehouses"" SET ""Code"" = 'WH-' || ""Id"" WHERE ""Code"" = '' OR ""Code"" IS NULL;
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Warehouses_CompanyId_Code"" ON ""Warehouses"" (""CompanyId"", ""Code"");
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Inventories_CompanyId_ProductId_WarehouseId"" ON ""Inventories"" (""CompanyId"", ""ProductId"", ""WarehouseId"");
                    ALTER TABLE IF EXISTS ""CreditNotes"" ADD COLUMN IF NOT EXISTS ""SaleId"" integer NULL;
                    CREATE INDEX IF NOT EXISTS ""IX_CreditNotes_CompanyId_SaleId"" ON ""CreditNotes"" (""CompanyId"", ""SaleId"");
                ");
                _logger.LogInformation("Migrations applied successfully");
                return Ok(new { result = "ok", message = "Migrations applied and schema verified" });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to apply migrations");
                return StatusCode(500, new { result = "error", message = "Failed to apply migrations", detail = ex.Message });
            }
        }

        [HttpPost("ensure-schema")]
        public async Task<IActionResult> EnsureSchema()
        {
            try
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE IF EXISTS ""Warehouses"" ADD COLUMN IF NOT EXISTS ""Code"" text NOT NULL DEFAULT '';
                    UPDATE ""Warehouses"" SET ""Code"" = 'WH-' || ""Id"" WHERE ""Code"" = '' OR ""Code"" IS NULL;
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Warehouses_CompanyId_Code"" ON ""Warehouses"" (""CompanyId"", ""Code"");
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Inventories_CompanyId_ProductId_WarehouseId"" ON ""Inventories"" (""CompanyId"", ""ProductId"", ""WarehouseId"");
                    ALTER TABLE IF EXISTS ""CreditNotes"" ADD COLUMN IF NOT EXISTS ""SaleId"" integer NULL;
                    CREATE INDEX IF NOT EXISTS ""IX_CreditNotes_CompanyId_SaleId"" ON ""CreditNotes"" (""CompanyId"", ""SaleId"");
                ");
                return Ok(new { result = "ok", message = "Schema verified and updated successfully" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { result = "error", message = ex.Message });
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
