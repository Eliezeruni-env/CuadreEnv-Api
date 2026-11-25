using System;
using System.ComponentModel.DataAnnotations;
using Onion.Common.Models;

namespace Onion.Domain.Appointments
{
    public class AppointmentRescheduleRequest : BaseEntity, ITenantEntity
    {
        public int CompanyId { get; set; }

        public int AppointmentId { get; set; }
        public virtual Appointment? Appointment { get; set; }

        public DateTime ProposedStartAt { get; set; }
        public DateTime ProposedEndAt { get; set; }

        [MaxLength(50)]
        public string RequestedBy { get; set; } = "Client"; // "Client" | "Business"

        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // "Pending" | "Accepted" | "Rejected" | "Expired"

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
