using Onion.DataAccess.Configurations.DependencyInjection;
using Onion.BussinesLogic.Configurations.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Globalization service used by controllers to obtain localized Error objects
builder.Services.AddSingleton<Onion.Common.Services.IGlobalizationService, Onion.Common.Services.GlobalizationService>();

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddRepositories(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddServices();
builder.Services.AddDomainServices();
builder.Services.AddValidators();
// Register HttpContextAccessor and JWT-based tenant provider for CompanyId extraction
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Onion.DataAccess.ITenantProvider, Onion.DataAccess.Tenant.JwtTenantProvider>();
// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? string.Empty;
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? string.Empty;
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
// Rate limiting for sensitive endpoints (login / refresh)
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("LoginPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 6,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        var resp = Onion.Common.Models.ApiResponse<object>.Fail("Too many requests", new[] { "RATE_LIMIT" });
        await context.HttpContext.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(resp), token);
    };
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("EveryOne", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Onion API v1");
    c.RoutePrefix = "swagger";
});

app.MapHealthChecks("/hc");
app.MapGet("/", () => Results.Ok(new { service = "Onion API", status = "ok" }));

app.UseHttpsRedirection();
app.UseCors("EveryOne");
// Global exception handler -> standardized API responses
app.UseAuthentication();
// Apply rate limiting middleware (policies defined in DI)
app.UseRateLimiter();
// Validate tenant claim presence for authenticated requests
app.UseMiddleware<Onion.Controllers.Middleware.TenantMiddleware>();
app.UseAuthorization();
// Global exception handler -> standardized API responses
app.UseMiddleware<Onion.Controllers.Middleware.ApiExceptionMiddleware>();
app.MapControllers();

app.Run();