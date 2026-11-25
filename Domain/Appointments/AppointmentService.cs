using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Onion.Common.Models;
using Onion.Domain.Products;

namespace Onion.Domain.Appointments
{
    public class AppointmentService : BaseEntity, ITenantEntity
    {
        public int CompanyId { get; set; }

        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        public int DurationMinutes { get; set; } = 30;
        public int BufferTimeMinutes { get; set; } = 0;
        public bool IsPubliclyBookable { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;

        [MaxLength(20)]
        public string? ColorHex { get; set; } = "#3B82F6";

        public virtual ICollection<AppointmentProfessionalService>? ProfessionalServices { get; set; }
    }
}
