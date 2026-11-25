using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Onion.Common.Models;
using Onion.Domain.Users;

namespace Onion.Domain.Appointments
{
    public class AppointmentProfessional : BaseEntity, ITenantEntity
    {
        public int CompanyId { get; set; }

        public int? UserId { get; set; }
        public virtual User? User { get; set; }

        [MaxLength(150)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? SpecialtyTitle { get; set; }

        public bool IsOwner { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;

        public virtual ICollection<AppointmentProfessionalService>? ProfessionalServices { get; set; }
        public virtual ICollection<AppointmentEmployeeAvailability>? Availabilities { get; set; }
        public virtual ICollection<AppointmentAvailabilityException>? Exceptions { get; set; }
    }
}
