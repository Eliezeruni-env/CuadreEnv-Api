namespace Onion.DataAccess.Clerk;

public sealed class ClerkOrganization
{
    public string ClerkOrganizationId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Slug { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ClerkOrganizationMember> Members { get; set; } = new List<ClerkOrganizationMember>();
    public ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
}
