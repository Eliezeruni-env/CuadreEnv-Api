using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Onion.BussinesLogic.Configurations.DependencyInjection;
using Onion.Common.Services;
using Onion.Controllers.Middleware;
using Onion.DataAccess.Configurations.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using crud_onion.Middleware;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("OnionCrud");
if (string.IsNullOrWhiteSpace(connectionString) && builder.Environment.IsProduction())
    throw new InvalidOperationException("ConnectionStrings:OnionCrud must be configured before starting in Production.");

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
        options.Filters.Add<Onion.Filters.AuditDeletionApprovalFilter>();
        options.Conventions.Add(new crud_onion.Authorization.ModuleAuthorizationConvention());
    })
    .AddJsonOptions(opts =>
    {
        // Prevent self-referencing loop serialization when EF entities include navigation properties
        opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        // Don't emit nulls to reduce payloads
        opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddHttpClient("Dgii", client =>
{
    var baseUrl = builder.Configuration["Dgii:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl)) client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Dgii:TimeoutSeconds", 15));
});
builder.Services.AddSingleton<Onion.BussinesLogic.Services.Concrete.ILicenseStartupValidator, Onion.BussinesLogic.Services.Concrete.LicenseStartupValidator>();

// Swagger configuration including Internal API key definition for /internal endpoints
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CuadreEnv API",
        Version = "v1",
        Description = "API de CuadreEnv SaaS"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
     Description = "Pegue un JWT local. Swagger agregará automáticamente el prefijo Bearer."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },

            },
            Array.Empty<string>()
        }
    });

    // Define API key security scheme for internal endpoints
    c.AddSecurityDefinition("InternalApiKey", new OpenApiSecurityScheme
    {
        Description = "Internal API Key required for calls to /internal endpoints. Provide in X-Internal-ApiKey header.",
        Name = "X-Internal-ApiKey",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
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
// CORS: allow frontend origin (development). Configure FRONTEND__URL in environment or fallback to http://localhost:3000
var frontendUrl = builder.Configuration["Frontend:Url"] ?? Environment.GetEnvironmentVariable("FRONTEND__URL") ?? "http://localhost:3000";
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
// HttpClient factory used by UM proxy example
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("Usm", client =>
{
    var baseUrl = builder.Configuration["Usm:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl)) client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Usm:TimeoutSeconds", 5));
});
// CompanyEntitlementService uses the scoped OnionDbContext for local USM reads.
// IMemoryCache remains singleton, so entitlement data is still shared across requests.
builder.Services.AddScoped<Onion.Common.Services.ICompanyEntitlementService, Onion.BussinesLogic.Services.Concrete.CompanyEntitlementService>();
builder.Services.AddScoped<Onion.BussinesLogic.Services.Abstract.IAccountStatusService, Onion.BussinesLogic.Services.Concrete.AccountStatusService>();
// Background queue and Caja (POS) registrations
builder.Services.AddSingleton<Onion.BussinesLogic.Background.InMemoryBackgroundQueue>();
builder.Services.AddSingleton<Onion.BussinesLogic.Background.IBackgroundQueue>(sp => sp.GetRequiredService<Onion.BussinesLogic.Background.InMemoryBackgroundQueue>());
builder.Services.AddHostedService<Onion.BussinesLogic.Background.BackgroundWorker>();
builder.Services.AddHostedService<Onion.BussinesLogic.Background.FiscalOutboxWorker>();
builder.Services.AddScoped<Onion.BussinesLogic.Services.Abstract.ICajaService, Onion.BussinesLogic.Services.Concrete.CajaService>();
// Register HttpContextAccessor and local JWT tenant provider for CompanyId extraction
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Onion.DataAccess.ITenantProvider, Onion.DataAccess.Tenant.JwtTenantProvider>();
if (builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(builder.Configuration["Internal:ApiKey"]))
    throw new InvalidOperationException("Internal:ApiKey must be configured in Production.");

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) && builder.Environment.IsProduction())
    throw new InvalidOperationException("Jwt:Key must be configured in Production.");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.MapInboundClaims = false;
         var jwtIssuer = builder.Configuration["Jwt:Issuer"];
         var jwtAudience = builder.Configuration["Jwt:Audience"];
         options.TokenValidationParameters = new TokenValidationParameters
         {
             ValidateIssuerSigningKey = true,
              IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(jwtKey) ? "dev-local-key-please-change-in-production-32chars!" : jwtKey)),
             ValidateIssuer = true, ValidIssuer = jwtIssuer,
             ValidateAudience = !string.IsNullOrWhiteSpace(jwtAudience), ValidAudience = jwtAudience,
             ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1),
             NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier, RoleClaimType = "role"
         };
    });

// Register ambient tenant provider for background jobs and CurrentUserService for web requests
builder.Services.AddSingleton<Onion.DataAccess.Tenant.AmbientTenantProvider>();

// Hosted service to mark overdue credits daily
builder.Services.AddHostedService<Onion.BussinesLogic.HostedServices.CreditOverdueHostedService>();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("LocalCompanyAdmin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin", "SuperAdmin", "SysAdmin");
    });
});
// Central authorization service (role/permission checks)
builder.Services.AddSingleton<Onion.Common.Authorization.IAuthorizationService, Onion.Common.Authorization.AuthorizationService>();
builder.Services.AddScoped<Onion.BussinesLogic.Services.Abstract.IRoleService, Onion.BussinesLogic.Services.Concrete.RoleService>();
builder.Services.AddScoped<Onion.BussinesLogic.Services.Abstract.IPermissionService, Onion.BussinesLogic.Services.Concrete.PermissionService>();
builder.Services.AddScoped<Onion.BussinesLogic.Services.Abstract.IDeletionApprovalService, Onion.BussinesLogic.Services.Concrete.DeletionApprovalService>();
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

// Production fails closed if USM cannot verify the license. A grace mode requires
// a signed-cache contract and is intentionally not enabled by this application.
await app.Services.GetRequiredService<Onion.BussinesLogic.Services.Concrete.ILicenseStartupValidator>()
    .ValidateAsync(app.Lifetime.ApplicationStopping);

app.UseStaticFiles();
app.UseSwagger();

// --- UPDATED SWAGGER UI CONFIGURATION ---
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cuadre Env v1");
    c.RoutePrefix = "swagger";
    c.EnablePersistAuthorization(); // Keeps you logged in across page refreshes
    // No custom JS injection. Swagger UI will use the generated OpenAPI JSON.
});
// ----------------------------------------

app.MapHealthChecks("/hc").AllowAnonymous();
app.MapGet("/", () => Results.Ok(new { service = "Onion API", status = "ok" })).AllowAnonymous();

// Force HTTPS only in non-development environments
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("DefaultCors");
// Global exception handler -> standardized API responses
app.UseAuthentication();
app.UseMiddleware<Onion.Middleware.ActiveAccountMiddleware>();
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
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Onion.DataAccess.OnionDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        // Log migration errors but do not stop the application startup so Swagger and routes can load.
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to apply EF migrations on startup. Startup will continue without applying migrations.");
    }
}

// Enable Swagger UI so API routes can be explored even if migrations failed
try
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CuadreEnv API v1");
        c.RoutePrefix = string.Empty; // serve swagger at app root
    });
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "Failed to enable Swagger middleware. Continuing without Swagger.");
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
