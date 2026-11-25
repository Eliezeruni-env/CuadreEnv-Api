using System;
using Onion.Common.Models;

namespace Onion.Domain.Appointments
{
    public class AppointmentEmployeeAvailability : BaseEntity, ITenantEntity
    {
        public int CompanyId { get; set; }

        public int ProfessionalId { get; set; }
        public virtual AppointmentProfessional? Professional { get; set; }

        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public TimeSpan? BreakStartTime { get; set; }
        public TimeSpan? BreakEndTime { get; set; }
        public bool IsWorkingDay { get; set; } = true;
    }
}
