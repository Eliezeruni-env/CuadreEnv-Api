using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace crud_onion.Authentication;

public sealed class ProjectAccessRequirement(string projectName) : IAuthorizationRequirement
{
    public string ProjectName { get; } = projectName;
}

public sealed class ProjectAccessHandler : AuthorizationHandler<ProjectAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProjectAccessRequirement requirement)
    {
        var hasProjectAccess = context.User
            .Claims
            .Where(claim => claim.Type is "projects" or "publicMetadata" or "public_metadata" or "publicMetadata.projects" or "public_metadata.projects")
            .SelectMany(claim => ReadProjectValues(claim.Value))
            .Any(project => string.Equals(project, requirement.ProjectName, StringComparison.Ordinal));

        if (hasProjectAccess)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }

    private static IEnumerable<string> ReadProjectValues(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var trimmed = value.Trim();
        if (trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                using var document = JsonDocument.Parse(trimmed);
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return document.RootElement.EnumerateArray()
                        .Where(item => item.ValueKind == JsonValueKind.String)
                        .Select(item => item.GetString()!)
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .ToArray();
                }
            }
            catch (JsonException)
            {
                return [];
            }
        }

        if (trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            try
            {
                using var document = JsonDocument.Parse(trimmed);
                if (document.RootElement.TryGetProperty("projects", out var projects) &&
                    projects.ValueKind == JsonValueKind.Array)
                {
                    return projects.EnumerateArray()
                        .Where(item => item.ValueKind == JsonValueKind.String)
                        .Select(item => item.GetString()!)
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .ToArray();
                }
            }
            catch (JsonException)
            {
                return [];
            }
        }

        return trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
