using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Onion.Common.Authorization;
using Onion.Common.Services;
using Onion.DataAccess;
using System.Text.Json;

namespace Onion.BussinesLogic.Services.Concrete;

public sealed class CompanyEntitlementService : ICompanyEntitlementService
{
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly OnionDbContext _db;
    private readonly ILogger<CompanyEntitlementService> _logger;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<int, SemaphoreSlim> _gates = new();

    public CompanyEntitlementService(IMemoryCache cache, IHttpClientFactory httpClientFactory, IConfiguration configuration, OnionDbContext db, ILogger<CompanyEntitlementService> logger)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _db = db;
        _logger = logger;
    }

    public async Task<CompanyEntitlements> GetAsync(int companyId, CancellationToken cancellationToken = default)
    {
        if (companyId <= 0) return Empty;
        var key = $"company-entitlements:{companyId}";
        if (_cache.TryGetValue(key, out CompanyEntitlements? cached) && cached is not null) return cached;

        var gate = _gates.GetOrAdd(companyId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(key, out cached) && cached is not null) return cached;
            var value = await LoadAsync(companyId, cancellationToken);
            var minutes = Math.Clamp(_configuration.GetValue("Usm:CacheMinutes", 10), 5, 15);
            _cache.Set(key, value, TimeSpan.FromMinutes(minutes));
            return value;
        }
        finally { gate.Release(); }
    }

    public void Invalidate(int companyId)
    {
        if (companyId > 0) _cache.Remove($"company-entitlements:{companyId}");
    }

        private static readonly HashSet<string> DefaultModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "SALES", "POS", "CASHREGISTER", "INVENTORY", "BILLING", "RECEIVABLES", "CUSTOMERS", "PURCHASES", "REPORTS", "AUDIT", "COMPANY"
    };

    private async Task<CompanyEntitlements> LoadAsync(int companyId, CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["Usm:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            var client = _httpClientFactory.CreateClient("Usm");
            try
            {
                var apiKey = _configuration["Usm:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    _logger.LogError("USM entitlement API key is not configured; denying module access for company {CompanyId}", companyId);
                    return Empty;
                }

                using var modulesRequest = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/internal/companies/{companyId}/modules");
                modulesRequest.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
                using var modulesResponse = await client.SendAsync(modulesRequest, cancellationToken);
                modulesResponse.EnsureSuccessStatusCode();
                var modulePayload = await modulesResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

                using var projectsRequest = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/internal/companies/{companyId}/projects");
                projectsRequest.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
                using var projectsResponse = await client.SendAsync(projectsRequest, cancellationToken);
                projectsResponse.EnsureSuccessStatusCode();
                var projectPayload = await projectsResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

                var modules = ReadModuleEntitlements(modulePayload);
                var projects = ReadProjectEntitlements(projectPayload);
                return new CompanyEntitlements(
                    ModuleCodes.Normalize(modules).ToHashSet(StringComparer.Ordinal),
                    projects);
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                _logger.LogError(ex, "USM entitlement lookup failed for company {CompanyId}; denying module access", companyId);
                return Empty;
            }
        }

        if (_configuration.GetValue("Usm:UseLocalDatabase", false))
            return await LoadLocalAsync(companyId, cancellationToken);

        return Empty;
    }

    private static IReadOnlyCollection<string> ReadModuleEntitlements(JsonElement payload)
    {
        var items = FindArray(payload);
        var modules = new List<string>();
        foreach (var item in items)
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                AddModule(modules, item.GetString());
                continue;
            }

            if (item.ValueKind != JsonValueKind.Object || TryGetBoolean(item, "enabled", out var enabled) && !enabled)
                continue;

            AddModule(modules,
                GetString(item, "code") ??
                GetString(item, "moduleCode") ??
                GetString(item, "moduleId") ??
                GetString(item, "id"));
        }

        return modules;
    }

    private static HashSet<string> ReadProjectEntitlements(JsonElement payload)
    {
        var data = GetProperty(payload, "data");
        if (data.ValueKind == JsonValueKind.Object)
        {
            var projectIds = GetProperty(data, "projectIds");
            if (projectIds.ValueKind == JsonValueKind.Array)
            {
                return projectIds.EnumerateArray()
                    .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : null)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
        }

        return FindArray(payload)
            .Select(item => item.ValueKind == JsonValueKind.String
                ? item.GetString()
                : GetString(item, "projectId") ?? GetString(item, "id"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<JsonElement> FindArray(JsonElement payload)
    {
        if (payload.ValueKind == JsonValueKind.Array)
            return payload.EnumerateArray().ToArray();

        var data = GetProperty(payload, "data");
        if (data.ValueKind == JsonValueKind.Array)
            return data.EnumerateArray().ToArray();

        if (data.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in new[] { "items", "modules", "projects" })
            {
                var nested = GetProperty(data, name);
                if (nested.ValueKind == JsonValueKind.Array)
                    return nested.EnumerateArray().ToArray();
            }
        }

        return Array.Empty<JsonElement>();
    }

    private static JsonElement GetProperty(JsonElement value, string propertyName)
    {
        if (value.ValueKind != JsonValueKind.Object)
            return default;

        foreach (var property in value.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        }

        return default;
    }

    private static string? GetString(JsonElement value, string propertyName)
    {
        var property = GetProperty(value, propertyName);
        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    private static bool TryGetBoolean(JsonElement value, string propertyName, out bool result)
    {
        var property = GetProperty(value, propertyName);
        if (property.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            result = property.GetBoolean();
            return true;
        }

        result = false;
        return false;
    }

    private static void AddModule(ICollection<string> modules, string? module)
    {
        if (!string.IsNullOrWhiteSpace(module))
            modules.Add(module);
    }

    private async Task<CompanyEntitlements> LoadLocalAsync(int companyId, CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("Usm") ?? _configuration.GetConnectionString("OnionCrud");
        if (string.IsNullOrWhiteSpace(connectionString)) return Empty;
        var modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var projects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT CONVERT(nvarchar(200), ModuleId) FROM CompanyModules WHERE CompanyId=@companyId AND Enabled=1";
            command.Parameters.Add(new SqlParameter("companyId", companyId));
            try { await using var reader = await command.ExecuteReaderAsync(cancellationToken); while (await reader.ReadAsync(cancellationToken)) modules.Add(reader.GetString(0)); }
            catch (SqlException ex) when (ex.Number == 208) { }
        }
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT CONVERT(nvarchar(200), ProjectId) FROM CompanyProjects WHERE CompanyId=@companyId";
            command.Parameters.Add(new SqlParameter("companyId", companyId));
            try { await using var reader = await command.ExecuteReaderAsync(cancellationToken); while (await reader.ReadAsync(cancellationToken)) projects.Add(reader.GetString(0)); }
            catch (SqlException ex) when (ex.Number == 208) { }
        }
        return new CompanyEntitlements(modules, projects);
    }

    private static readonly CompanyEntitlements Empty = new(new HashSet<string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
}
