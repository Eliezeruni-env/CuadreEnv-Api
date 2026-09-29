namespace Onion.DataAccess.Clerk;

public sealed class ClerkUser
{
    public string ClerkUserId { get; set; } = null!;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ClerkOrganizationMember> OrganizationMemberships { get; set; } = new List<ClerkOrganizationMember>();
}
