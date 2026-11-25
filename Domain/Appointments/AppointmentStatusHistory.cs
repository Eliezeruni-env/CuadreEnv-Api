using System.ComponentModel.DataAnnotations;
using Onion.Common.Models;

namespace Onion.Domain.Appointments
{
    public class AppointmentStatusHistory : BaseEntity, ITenantEntity
    {
        public int CompanyId { get; set; }

        public int AppointmentId { get; set; }
        public virtual Appointment? Appointment { get; set; }

        public AppointmentStatus PreviousStatus { get; set; }
        public AppointmentStatus NewStatus { get; set; }

        [MaxLength(100)]
        public string? ChangedByUserId { get; set; }

        [MaxLength(500)]
        public string? ChangeReason { get; set; }
    }
}
