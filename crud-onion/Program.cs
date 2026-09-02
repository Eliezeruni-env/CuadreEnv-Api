using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
// Using Microsoft.OpenApi.Models removed to avoid missing/unstable OpenAPI package types
using Onion.BussinesLogic.Configurations.DependencyInjection;
using Onion.Common.Services;
using Onion.Controllers.Middleware;
using Onion.DataAccess.Configurations.DependencyInjection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Ensure the app listens on the development ports commonly used by frontends.
// Allow override via ASPNETCORE_URLS or PORT environment variables. Default to 8080 and 5000.
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? $"http://*:{port};http://*:5000";
builder.WebHost.UseUrls(urls);

builder.Services.AddSingleton<Onion.Common.Services.IGlobalizationService, Onion.Common.Services.GlobalizationService>();
builder.Services.AddControllers(options =>
    {
        // Prepend global API version prefix to all controller routes
        options.Conventions.Insert(0, new Onion.Common.Mvc.RoutePrefixConvention("v1"));
        // Return consistent ApiResponse on model validation failures to simplify frontend handling
        options.Filters.Add(new Onion.Controllers.Filters.ValidateModelAttribute());
    })
    .AddJsonOptions(opts =>
    {
        // Prevent self-referencing loop serialization when EF entities include navigation properties
        opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        // Don't emit nulls to reduce payloads
        opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Swagger configuration including Internal API key definition for /internal endpoints
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Define API key security scheme for internal endpoints
    c.AddSecurityDefinition("InternalApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Internal API Key required for calls to /internal endpoints. Provide in X-Internal-ApiKey header.",
        Name = "X-Internal-ApiKey",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "apiKey"
    });

    // Apply the custom operation filters for internal endpoints
    c.OperationFilter<Onion.Controllers.Swagger.InternalOperationFilter>();
    c.OperationFilter<Onion.Controllers.Swagger.InternalExamplesOperationFilter>();
});

// ----------------------------------------------------------

builder.Services.AddRepositories(builder.Configuration);
// Register Caja module
builder.Services.AddCajaModule();
builder.Services.AddHealthChecks();
builder.Services.AddServices();
builder.Services.AddDomainServices();
builder.Services.AddValidators();
// HttpClient factory used by UM proxy example
builder.Services.AddHttpClient();
// Background queue and Caja (POS) registrations
builder.Services.AddSingleton<Onion.BussinesLogic.Background.InMemoryBackgroundQueue>();
builder.Services.AddSingleton<Onion.BussinesLogic.Background.IBackgroundQueue>(sp => sp.GetRequiredService<Onion.BussinesLogic.Background.InMemoryBackgroundQueue>());
builder.Services.AddHostedService<Onion.BussinesLogic.Background.BackgroundWorker>();
builder.Services.AddScoped<Onion.BussinesLogic.Services.Abstract.ICajaService, Onion.BussinesLogic.Services.Concrete.CajaService>();
// Register HttpContextAccessor and JWT-based tenant provider for CompanyId extraction
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Onion.DataAccess.ITenantProvider, Onion.DataAccess.Tenant.JwtTenantProvider>();
// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    // Allow a fallback key when NOT running in Production (covers Development and other local envs).
    // In Production the key must be provided via configuration or environment variable.
    if (!builder.Environment.IsProduction())
    {
        jwtKey = "dev-local-key-please-change-in-production-32chars!"; // must be non-empty and sufficiently long
        Console.WriteLine("Warning: Jwt:Key not configured. Using non-production fallback key.");
    }
    else
    {
        throw new InvalidOperationException("Jwt:Key missing. Set configuration or environment variable 'Jwt:Key' before starting the app in production.");
    }
}
// Ensure configuration exposes the effective key so other components reading IConfiguration get the same value
try
{
    builder.Configuration["Jwt:Key"] = jwtKey;
}
catch
{
    // If configuration is not writable (unlikely), log a warning but continue since jwtKey variable is used below
    Console.WriteLine("Warning: unable to write effective Jwt:Key into configuration; relying on local variable.");
}
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

// Register ambient tenant provider for background jobs and CurrentUserService for web requests
builder.Services.AddSingleton<Onion.DataAccess.Tenant.AmbientTenantProvider>();

// Hosted service to mark overdue credits daily
builder.Services.AddHostedService<Onion.BussinesLogic.HostedServices.CreditOverdueHostedService>();

builder.Services.AddAuthorization();
// Central authorization service (role/permission checks)
builder.Services.AddSingleton<Onion.Common.Authorization.IAuthorizationService, Onion.Common.Authorization.AuthorizationService>();
// Register email service (production default). Tests may replace this registration in WebApplicationFactory.
builder.Services.AddTransient<IEmailService, SmtpEmailService>();
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
        await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(resp), token);
    };
});

builder.Services.AddCors(options =>
{
    // Restrictive CORS policy for demo: allow localhost:4200 and a placeholder production origin
    options.AddPolicy("DefaultCors", policy =>
    {
        // Allow common dev origins (Angular dev server, API running on http:8080, IIS Express https)
        policy.WithOrigins(
            "http://localhost:4200",
            "http://localhost:5160",
            "http://localhost:8080",
            "https://localhost:7060",
            "https://localhost:44324",
            "https://your-production-frontend.example.com")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


var app = builder.Build();
app.UseStaticFiles();
app.UseSwagger();

// --- UPDATED SWAGGER UI CONFIGURATION ---
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Onion API v1");
    c.RoutePrefix = "swagger";
    c.EnablePersistAuthorization(); // Keeps you logged in across page refreshes
    // No custom JS injection. Swagger UI will use the generated OpenAPI JSON.
});
// ----------------------------------------

app.MapHealthChecks("/hc");
app.MapGet("/", () => Results.Ok(new { service = "Onion API", status = "ok" }));

// Force HTTPS only in non-development environments
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("DefaultCors");
// Global exception handler -> standardized API responses
app.UseAuthentication();
// Apply rate limiting middleware (policies defined in DI)
app.UseRateLimiter();
// Internal API authentication (API key or SuperAdmin role)
app.UseMiddleware<InternalApiAuthMiddleware>();
// Validate tenant claim presence for authenticated requests
app.UseMiddleware<Onion.Controllers.Middleware.TenantMiddleware>();
app.UseAuthorization();
// Global exception handler -> standardized API responses
app.UseMiddleware<ApiExceptionMiddleware>();
app.MapControllers();

// Optionally apply EF migrations on startup when configured. This is disabled by default
// to avoid accidental schema changes in production. Tests or dev may enable via configuration.
if (builder.Configuration.GetValue<bool>("ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<Onion.DataAccess.OnionDbContext>();
    db.Database.Migrate();
}

// Optional demo data seeding when enabled explicitly via configuration
if (builder.Configuration.GetValue<bool>("RunDemoSeedOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    // Run idempotent demo seeder
    await Onion.DataAccess.Seed.DemoSeeder.SeedAsync(services);
}

app.Run();
