using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Onion.DataAccess;
using System.Text.Json;

namespace Onion.Controllers
{
    public class LeadDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? JobRole { get; set; }
        public string? CompanyName { get; set; }
        public string? Rnc { get; set; }
        public string? BusinessType { get; set; }
        public string? Branches { get; set; }
        public string? Registers { get; set; }
        public string? City { get; set; }
        public string[]? Modules { get; set; }
        public string? CurrentSystem { get; set; }
        public string? Timeline { get; set; }
        public string? ContactPref { get; set; }
        public string? Message { get; set; }
        public string? Source { get; set; }
        public object? Utm { get; set; }
        public string? Website { get; set; } // honeypot
    }

    [ApiController]
    [Route("api/leads")]
    public class LeadsController : ControllerBase
    {
        private readonly OnionDbContext _context;
        private readonly ILogger<LeadsController> _logger;

        public LeadsController(OnionDbContext context, ILogger<LeadsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateLead([FromBody] LeadDto dto)
        {
            // Honeypot anti-spam check
            if (!string.IsNullOrWhiteSpace(dto.Website))
            {
                return Ok(new { ok = true, id = 999 });
            }

            if (string.IsNullOrWhiteSpace(dto.FullName) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Phone) ||
                string.IsNullOrWhiteSpace(dto.CompanyName) ||
                string.IsNullOrWhiteSpace(dto.BusinessType))
            {
                return BadRequest(new { ok = false, message = "Por favor completa todos los campos requeridos." });
            }

            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
                var modulesJson = dto.Modules != null ? JsonSerializer.Serialize(dto.Modules) : "[]";
                var utmJson = dto.Utm != null ? JsonSerializer.Serialize(dto.Utm) : "null";

                var sql = @"
                    INSERT INTO ""Leads"" (
                        ""FullName"", ""Email"", ""Phone"", ""JobRole"", ""CompanyName"",
                        ""Rnc"", ""BusinessType"", ""Branches"", ""Registers"", ""City"",
                        ""Modules"", ""CurrentSystem"", ""Timeline"", ""ContactPref"", ""Message"",
                        ""Source"", ""Utm"", ""Ip"", ""UserAgent""
                    ) VALUES (
                        {0}, {1}, {2}, {3}, {4},
                        {5}, {6}, {7}, {8}, {9},
                        {10}::jsonb, {11}, {12}, {13}, {14},
                        {15}, {16}::jsonb, {17}, {18}
                    ) RETURNING ""Id"";";

                // Direct insert into PostgreSQL Leads table
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO ""Leads"" (
                        ""FullName"", ""Email"", ""Phone"", ""JobRole"", ""CompanyName"",
                        ""Rnc"", ""BusinessType"", ""Branches"", ""Registers"", ""City"",
                        ""Modules"", ""CurrentSystem"", ""Timeline"", ""ContactPref"", ""Message"",
                        ""Source"", ""Utm"", ""Ip"", ""UserAgent""
                    ) VALUES (
                        @fullName, @email, @phone, @jobRole, @companyName,
                        @rnc, @businessType, @branches, @registers, @city,
                        @modules::jsonb, @currentSystem, @timeline, @contactPref, @message,
                        @source, @utm::jsonb, @ip, @userAgent
                    ) RETURNING ""Id"";";

                void AddParam(string name, object? val)
                {
                    var p = cmd.CreateParameter();
                    p.ParameterName = name;
                    p.Value = val ?? DBNull.Value;
                    cmd.Parameters.Add(p);
                }

                AddParam("@fullName", dto.FullName);
                AddParam("@email", dto.Email);
                AddParam("@phone", dto.Phone);
                AddParam("@jobRole", dto.JobRole);
                AddParam("@companyName", dto.CompanyName);
                AddParam("@rnc", dto.Rnc);
                AddParam("@businessType", dto.BusinessType);
                AddParam("@branches", dto.Branches);
                AddParam("@registers", dto.Registers);
                AddParam("@city", dto.City);
                AddParam("@modules", modulesJson);
                AddParam("@currentSystem", dto.CurrentSystem);
                AddParam("@timeline", dto.Timeline);
                AddParam("@contactPref", dto.ContactPref ?? "whatsapp");
                AddParam("@message", dto.Message);
                AddParam("@source", dto.Source ?? "landing");
                AddParam("@utm", utmJson);
                AddParam("@ip", ip);
                AddParam("@userAgent", userAgent);

                if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    await _context.Database.OpenConnectionAsync();

                var scalar = await cmd.ExecuteScalarAsync();
                var newId = Convert.ToInt32(scalar);

                _logger.LogInformation("Nuevo lead registrado exitosamente: #{Id} {FullName} ({Company})", newId, dto.FullName, dto.CompanyName);

                return Ok(new { ok = true, id = newId, message = "Prospecto registrado exitosamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar prospecto desde la landing page.");
                return StatusCode(500, new { ok = false, message = "Error al procesar la solicitud en el servidor." });
            }
        }
    }
}
