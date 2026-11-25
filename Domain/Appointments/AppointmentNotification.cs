using System;
using System.ComponentModel.DataAnnotations;
using Onion.Common.Models;

namespace Onion.Domain.Appointments
{
    public class AppointmentNotification : BaseEntity, ITenantEntity
    {
        public int CompanyId { get; set; }

        public int AppointmentId { get; set; }
        public virtual Appointment? Appointment { get; set; }

        [MaxLength(50)]
        public string Channel { get; set; } = "WhatsApp"; // "WhatsApp" | "Email"

        [MaxLength(50)]
        public string TriggerType { get; set; } = "Confirmation"; // "Confirmation" | "Reminder24h" | "Reminder1h" | "Rescheduled" | "Cancelled"

        public DateTime SentAt { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Sent"; // "Sent" | "Failed"

        [MaxLength(500)]
        public string? PayloadPreview { get; set; }
    }
}
