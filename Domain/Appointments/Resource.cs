using System.ComponentModel.DataAnnotations;
using Onion.Domain;

namespace Onion.Domain.Appointments
{
    public class Resource : BaseEntity, Onion.Common.Models.ITenantEntity
    {
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Type { get; set; }

        public bool IsActive { get; set; } = true;

        // Tenant scoping
        public int CompanyId { get; set; }

        // Navigation
        public ICollection<Availability>? Availabilities { get; set; }
    }
}
