using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Onion.Common.Services;
using Onion.DataAccess;

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
                var modules = await client.GetFromJsonAsync<List<EntitlementItem>>($"api/companies/{companyId}/modules", cancellationToken) ?? new();
                var projects = await client.GetFromJsonAsync<List<EntitlementItem>>($"api/companies/{companyId}/projects", cancellationToken) ?? new();
                return new CompanyEntitlements(
                    modules.Where(x => x.Enabled != false).Select(x => x.Code ?? x.Id ?? string.Empty).Where(x => x.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase),
                    projects.Where(x => x.Enabled != false).Select(x => x.Code ?? x.Id ?? string.Empty).Where(x => x.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "USM entitlement lookup failed for company {CompanyId}; denying module access", companyId);
                return Empty;
            }
        }

        if (_configuration.GetValue("Usm:UseLocalDatabase", false))
            return await LoadLocalAsync(companyId, cancellationToken);

        return Empty;
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
    private sealed record EntitlementItem(string? Id, string? Code, bool? Enabled);
}
