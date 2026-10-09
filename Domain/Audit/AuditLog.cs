using System;
namespace Onion.Domain.Audit
{
    public class AuditLog : Onion.Domain.BaseEntity
    {
        public int? CompanyId { get; set; }
        public int? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserRole { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string PerformedBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? AuditPayload { get; set; }
        public string? Details { get; set; }
    }
}
