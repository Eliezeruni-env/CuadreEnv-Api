using System.ComponentModel.DataAnnotations;

namespace Onion.Domain.Appointments
{
    public enum AppointmentStatus
    {
        Pending = 0,
        Confirmed = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }

    public class Appointment : BaseEntity, Onion.Common.Models.ITenantEntity
    {
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        // Relations
        public int? CustomerId { get; set; }
        public int? ServiceId { get; set; }
        public int? ResourceId { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Tenant scoping
        public int CompanyId { get; set; }
    }
}
