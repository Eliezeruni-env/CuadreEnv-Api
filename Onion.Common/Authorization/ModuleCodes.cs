using System.Text.Json;

namespace Onion.Common.Authorization;

public static class ModuleCodes
{
    public const string All = "*";

    private static readonly IReadOnlyDictionary<string, string> Aliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sales"] = "sales", ["sale"] = "sales",
            ["dashboard"] = "reports", ["metrics"] = "reports",
            ["inventory"] = "inventory",
            ["inventory-stock"] = "inventory", ["inventory-entries"] = "inventory", ["inventory-outlets"] = "inventory",
            ["inventory-transfers"] = "inventory", ["inventory-warehouses"] = "inventory", ["inventory-manage-requests"] = "company",
            ["billing"] = "billing", ["subscription"] = "billing",
            ["cashregister"] = "cashregister", ["cash_register"] = "cashregister", ["cash-register"] = "cashregister", ["cash"] = "cashregister", ["pos"] = "cashregister",
            ["customers"] = "customers", ["customer"] = "customers",
            ["receivables"] = "receivables", ["receivable"] = "receivables", ["accountreceivable"] = "receivables",
            ["credit-notes"] = "sales", ["fraud-guardian"] = "audit", ["services"] = "company",
            ["purchases"] = "purchases", ["purchase"] = "purchases",
            ["purchases-suppliers"] = "purchases", ["purchases-orders"] = "purchases", ["purchases-receipts"] = "purchases",
            ["payments"] = "sales", ["products"] = "sales", ["products-settings"] = "sales",
            ["billing-reports-dgii"] = "reports",
            ["audit"] = "audit",
            ["reports"] = "reports", ["report"] = "reports",
            ["company"] = "company",
            ["profile"] = "company", ["users"] = "company", ["admin-roles"] = "company", ["admin-roles-matrix"] = "company",
            ["admin-approvals"] = "audit", ["company-settings"] = "company", ["mobile-pos"] = "cashregister",
            // Legacy names retained only as aliases to a canonical module.
            ["user_management"] = "company", ["user-management"] = "company", ["rbac"] = "company",
            ["appointments"] = "company", ["projects"] = "company", ["proyectos"] = "company"
        };

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var code = value.Trim();
        if (code == All) return All;
        return Aliases.TryGetValue(code, out var canonical) ? canonical : null;
    }

    public static IReadOnlyCollection<string> Normalize(IEnumerable<string>? values)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        if (values is null) return result.ToArray();
        foreach (var value in values)
        {
            var normalized = Normalize(value);
            if (normalized == All) return new[] { All };
            if (normalized is not null) result.Add(normalized);
        }
        return result.ToArray();
    }

    public static IReadOnlyCollection<string> FromClaim(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Array.Empty<string>();
        try
        {
            if (value.TrimStart().StartsWith("[", StringComparison.Ordinal))
                return Normalize(JsonSerializer.Deserialize<string[]>(value) ?? Array.Empty<string>());
        }
        catch (JsonException) { return Array.Empty<string>(); }
        return Normalize(value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    public static IReadOnlyCollection<string> FromClaimPreservingIds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Array.Empty<string>();
        try
        {
            if (value.TrimStart().StartsWith("[", StringComparison.Ordinal) ||
                value.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                using var document = JsonDocument.Parse(value);
                return ReadAssignedIds(document.RootElement);
            }
        }
        catch (JsonException) { return Array.Empty<string>(); }

        return PreserveIds(value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static IReadOnlyCollection<string> ReadAssignedIds(JsonElement value)
    {
        var result = new List<string>();
        ReadAssignedIds(value, result);
        return PreserveIds(result);
    }

    private static void ReadAssignedIds(JsonElement value, ICollection<string> result)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                result.Add(value.GetString() ?? string.Empty);
                break;
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray()) ReadAssignedIds(item, result);
                break;
            case JsonValueKind.Object:
                if (value.TryGetProperty("data", out var data)) ReadAssignedIds(data, result);
                else
                {
                    foreach (var property in new[] { "moduleId", "moduleCode", "code", "id" })
                    {
                        if (value.TryGetProperty(property, out var moduleId) && moduleId.ValueKind == JsonValueKind.String)
                        {
                            result.Add(moduleId.GetString() ?? string.Empty);
                            break;
                        }
                    }
                }
                break;
        }
    }

    private static IReadOnlyCollection<string> PreserveIds(IEnumerable<string> values) => values
        .Select(value => value?.Trim())
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.Ordinal)
        .ToArray()!;

    public static IReadOnlyCollection<string> FromJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Array.Empty<string>();
        try
        {
            return Normalize(JsonSerializer.Deserialize<string[]>(value) ?? Array.Empty<string>());
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}
