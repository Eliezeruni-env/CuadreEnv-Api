namespace Onion.DataAccess.Clerk;

public sealed class ClerkOrganizationMember
{
    public string ClerkUserId { get; set; } = null!;
    public string ClerkOrganizationId { get; set; } = null!;
    public string Role { get; set; } = null!;
    public DateTime UpdatedAt { get; set; }

    public ClerkUser User { get; set; } = null!;
    public ClerkOrganization Organization { get; set; } = null!;
}
