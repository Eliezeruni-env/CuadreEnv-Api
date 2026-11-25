using System;
using System.ComponentModel.DataAnnotations;
using Onion.Common.Models;

namespace Onion.Domain.Appointments
{
    public class AppointmentAvailabilityException : BaseEntity, ITenantEntity
    {
        public int CompanyId { get; set; }

        public int ProfessionalId { get; set; }
        public virtual AppointmentProfessional? Professional { get; set; }

        public DateTime Date { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public bool IsAllDay { get; set; } = true;

        [MaxLength(250)]
        public string? Reason { get; set; }
    }
}
