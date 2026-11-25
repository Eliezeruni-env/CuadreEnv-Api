using System.ComponentModel.DataAnnotations;
using Onion.Common.Models;

namespace Onion.Domain.Appointments
{
    public class BookingPageSettings : BaseEntity, ITenantEntity
    {
        public int CompanyId { get; set; }

        [MaxLength(100)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(200)]
        public string BusinessName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }

        [MaxLength(20)]
        public string BrandColorHex { get; set; } = "#111827";

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? WhatsAppNumber { get; set; }

        public bool IsPublicActive { get; set; } = true;
    }
}
