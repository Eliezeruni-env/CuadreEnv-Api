using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Onion.DataAccess;
using Onion.DataAccess.Clerk;

namespace crud_onion.Webhooks;

public sealed class ClerkWebhookService(OnionDbContext db)
{
    public async Task ProcessAsync(string eventType, JsonElement data, CancellationToken cancellationToken)
    {
        switch (eventType)
        {
            case "user.created":
            case "user.updated":
                await UpsertUserAsync(data, cancellationToken);
                break;
            case "organizationMembership.created":
            case "organizationMembership.updated":
                await UpsertMembershipAsync(data, cancellationToken);
                break;
            case "organizationMembership.deleted":
                await DeleteMembershipAsync(data, cancellationToken);
                break;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertUserAsync(JsonElement data, CancellationToken ct)
    {
        var id = String(data, "id");
        if (string.IsNullOrWhiteSpace(id)) return;
        var user = await db.ClerkUsers.FindAsync([id], ct) ?? new ClerkUser { ClerkUserId = id };
        user.Email = data.TryGetProperty("email_addresses", out var emails) && emails.GetArrayLength() > 0
            ? String(emails[0], "email_address") : user.Email;
        user.FirstName = String(data, "first_name");
        user.LastName = String(data, "last_name");
        user.ImageUrl = String(data, "image_url");
        user.UpdatedAt = DateTime.UtcNow;
        if (db.Entry(user).State == EntityState.Detached) db.ClerkUsers.Add(user);
    }

    private async Task UpsertMembershipAsync(JsonElement data, CancellationToken ct)
    {
        var userId = String(data, "public_user_data", "user_id") ?? String(data, "public_user_data", "id") ?? String(data, "user_id");
        var organizationId = String(data, "organization", "id") ?? String(data, "organization_id");
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(organizationId)) return;

        var user = await db.ClerkUsers.FindAsync([userId], ct) ?? new ClerkUser { ClerkUserId = userId, UpdatedAt = DateTime.UtcNow };
        if (db.Entry(user).State == EntityState.Detached) db.ClerkUsers.Add(user);
        var organization = await db.ClerkOrganizations.FindAsync([organizationId], ct) ?? new ClerkOrganization { ClerkOrganizationId = organizationId, Name = String(data, "organization", "name") ?? organizationId, UpdatedAt = DateTime.UtcNow };
        organization.Name = String(data, "organization", "name") ?? organization.Name;
        organization.Slug = String(data, "organization", "slug") ?? organization.Slug;
        organization.UpdatedAt = DateTime.UtcNow;
        if (db.Entry(organization).State == EntityState.Detached) db.ClerkOrganizations.Add(organization);

        var member = await db.ClerkOrganizationMembers.FindAsync([userId, organizationId], ct)
            ?? new ClerkOrganizationMember { ClerkUserId = userId, ClerkOrganizationId = organizationId };
        member.Role = String(data, "role") ?? "org:member";
        member.UpdatedAt = DateTime.UtcNow;
        if (db.Entry(member).State == EntityState.Detached) db.ClerkOrganizationMembers.Add(member);
    }

    private async Task DeleteMembershipAsync(JsonElement data, CancellationToken ct)
    {
        var userId = String(data, "public_user_data", "user_id") ?? String(data, "user_id");
        var organizationId = String(data, "organization", "id") ?? String(data, "organization_id");
        if (userId is null || organizationId is null) return;
        var member = await db.ClerkOrganizationMembers.FindAsync([userId, organizationId], ct);
        if (member is not null) db.ClerkOrganizationMembers.Remove(member);
    }

    private static string? String(JsonElement element, params string[] path)
    {
        foreach (var property in path)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out element)) return null;
        }
        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }
}
