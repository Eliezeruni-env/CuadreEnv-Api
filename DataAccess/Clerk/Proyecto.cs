namespace Onion.DataAccess.Clerk;

public sealed class Proyecto
{
    public long Id { get; set; }
    public string OrganizationId { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public DateTime CreatedAt { get; set; }

    public ClerkOrganization Organization { get; set; } = null!;
}
