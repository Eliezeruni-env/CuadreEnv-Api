using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Dtos;
using Microsoft.AspNetCore.Authorization;
using Onion.Common.Authorization;
using System.Security.Claims;
using Onion.BussinesLogic.Services.Abstract;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth)
        {
            _auth = auth;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto req)
        {
            var user = await _auth.RegisterAsync(req);
            // Return serialized JSON string to avoid pipeline async serialization edge-cases in some test hosts
            var resp = Onion.Common.Models.ApiResponse<Onion.BussinesLogic.Dtos.UserResponseDto>.Ok(user, "User registered");
            var json = System.Text.Json.JsonSerializer.Serialize(resp, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            return Content(json, "application/json");
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto req)
        {
            var tokens = await _auth.LoginAsync(req);
            var resp = tokens;
            var json = System.Text.Json.JsonSerializer.Serialize(resp, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            return Content(json, "application/json");
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto req)
        {
            var tokens = await _auth.RefreshTokenAsync(req);
            var resp = tokens;
            var json = System.Text.Json.JsonSerializer.Serialize(resp, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            return Content(json, "application/json");
        }

        [HttpPost("revoke")]
        [AllowAnonymous]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Revoke([FromBody] RevokeRequestDto req)
        {
            await _auth.RevokeTokenAsync(req);
            return NoContent();
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            var modules = User.Claims
                .Where(c => string.Equals(c.Type, "allowedModules", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(c.Type, "allowed_modules", StringComparison.OrdinalIgnoreCase))
                .SelectMany(c => ModuleCodes.FromClaim(c.Value))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var idValue = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _ = int.TryParse(idValue, out var userId);

            return Ok(new
            {
                user = new
                {
                    id = userId,
                    email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value,
                    role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value,
                    allowedModules = modules
                }
            });
        }

        [Authorize]
        [HttpGet("/v1/auth/me/modules")]
        [HttpGet("/api/me/modules")]
        public async Task<IActionResult> Modules(
            [FromServices] Onion.Common.Services.ICompanyEntitlementService entitlements,
            [FromServices] IUserService users)
        {
            var idValue = User.FindFirst("sub")?.Value ??
                          User.FindFirst("userId")?.Value ??
                          User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idValue, out var userId) || userId <= 0)
                return Unauthorized();

            var currentUser = await users.GetByIdAsync(userId);
            if (currentUser is null || !currentUser.Active || currentUser.IsDeleted)
                return Unauthorized();

            var assignedModules = ModuleCodes.FromClaimPreservingIds(currentUser.AllowedModulesJson).ToArray();

            var isGlobalAdministrator = currentUser.IsSuperUser ||
                string.Equals(currentUser.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

            IReadOnlyCollection<string> licensedModules;
            if (isGlobalAdministrator)
            {
                licensedModules = new[] { ModuleCodes.All };
            }
            else
            {
                var companyClaim = User.FindFirst("companyId") ?? User.FindFirst("CompanyId");
                licensedModules = int.TryParse(companyClaim?.Value, out var companyId) && companyId > 0
                    ? ModuleCodes.Normalize((await entitlements.GetAsync(companyId)).Modules)
                    : Array.Empty<string>();
            }

            var hasAllAssignedModules = assignedModules.Contains(ModuleCodes.All, StringComparer.Ordinal);
            var hasAllLicensedModules = licensedModules.Contains(ModuleCodes.All, StringComparer.Ordinal);
            var effectiveModules = hasAllAssignedModules && hasAllLicensedModules
                ? new[] { ModuleCodes.All }
                : hasAllAssignedModules
                    ? licensedModules.ToArray()
                    : hasAllLicensedModules
                        ? assignedModules
                        : assignedModules
                            .Where(assigned => licensedModules.Contains(ModuleCodes.Normalize(assigned), StringComparer.Ordinal))
                            .ToArray();

            return Ok(new
            {
                assignedModules,
                licensedModules,
                modules = effectiveModules,
                hasAllModules = effectiveModules.Contains(ModuleCodes.All, StringComparer.Ordinal)
            });
        }

        [Authorize]
        [HttpPost("revoke-all")]
        public async Task<IActionResult> RevokeAll()
        {
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var userId))
                return BadRequest(Onion.Common.Models.ApiResponse<string>.Fail("UserId claim missing or invalid"));

            await _auth.RevokeAllTokensAsync(userId);
            return NoContent();
        }

        // Development-only helper: return a test JWT containing the provided companyId.
        // Enabled only when the app runs in Development environment. Accepts JSON body { companyId, userId?, email? }.
        [HttpPost("dev/token")]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult DevToken([FromServices] Microsoft.Extensions.Configuration.IConfiguration config,
                                      [FromServices] Microsoft.AspNetCore.Hosting.IWebHostEnvironment env,
                                      [FromBody] DevTokenRequest req)
        {
            if (!env.IsDevelopment())
            {
                // Endpoint intentionally disabled outside Development environment.
                return NotFound();
            }

            if (req == null || (req.CompanyId <= 0 && !req.IsSuperUser))
                return BadRequest(new { error = "companyId required and must be > 0" });

            var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
            var issuer = config["Jwt:Issuer"] ?? string.Empty;
            var audience = config["Jwt:Audience"] ?? string.Empty;

            var claims = new List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.NameIdentifier, (req.UserId ?? 9999).ToString()),
                new(System.Security.Claims.ClaimTypes.Email, req.Email ?? "dev@local"),
                new(System.Security.Claims.ClaimTypes.Role, "Admin"),
                new("role", "Admin")
            };
            if (req.CompanyId > 0)
            {
                claims.Add(new System.Security.Claims.Claim("companyId", req.CompanyId.ToString()));
                claims.Add(new System.Security.Claims.Claim("CompanyId", req.CompanyId.ToString()));
            }
            if (req.IsSuperUser)
                claims.Add(new System.Security.Claims.Claim("isSuperUser", "true"));

            var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: creds);

            var access = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
            return Ok(new TokenResponseDto(access, string.Empty));
        }
    }

    public record RegisterRequest(string Email, string Password);
    public record LoginRequest(string Email, string Password);
    public record RefreshRequest(string RefreshToken);
    public record RevokeRequest(string RefreshToken);
}

public record DevTokenRequest(int CompanyId, int? UserId = null, string? Email = null, bool IsSuperUser = false);
