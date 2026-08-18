using Onion.Domain;

namespace Onion.Domain.Appointments
{
    public class Availability : BaseEntity, Onion.Common.Models.ITenantEntity
    {
        public int ResourceId { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public bool IsBlocked { get; set; } = false; // true for blocked slots

        // Tenant scoping
        public int CompanyId { get; set; }

        // Navigation
        public Resource? Resource { get; set; }
    }
}
