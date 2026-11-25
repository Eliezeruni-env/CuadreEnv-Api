using Onion.Common.Models;

namespace Onion.Domain.Appointments
{
    public class AppointmentProfessionalService : BaseEntity, ITenantEntity
    {
        public int CompanyId { get; set; }

        public int ProfessionalId { get; set; }
        public virtual AppointmentProfessional? Professional { get; set; }

        public int AppointmentServiceId { get; set; }
        public virtual AppointmentService? AppointmentService { get; set; }

        public int? CustomDurationMinutes { get; set; }
        public decimal? CustomPrice { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
