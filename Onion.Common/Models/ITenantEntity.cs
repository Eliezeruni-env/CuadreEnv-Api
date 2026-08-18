namespace Onion.Common.Models
{
    // Marker interface for entities that belong to a tenant/company
    public interface ITenantEntity
    {
        int CompanyId { get; set; }
    }
}
