using System;

namespace Onion.Domain.Audit
{
    public class FraudGuardianIncident : BaseEntity
    {
        public int CompanyId { get; set; }
        public string IncidentCode { get; set; } = string.Empty; // e.g. ALT-101
        public string Type { get; set; } = string.Empty; // EXCESSIVE_CANCELLATIONS, DRAWER_KICK_NO_SALE, SUSPICIOUS_CONSECUTIVE_DISCOUNTS, EXCESSIVE_CASH_DISCREPANCY
        public string Severity { get; set; } = "Moderada"; // Crítica, Moderada, Baja
        public string SeverityCode { get; set; } = "moderate"; // critical, moderate, low
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty; // e.g. Cajero #104
        public int? CashierId { get; set; }
        public string? CashierName { get; set; }
        public int? CashRegisterId { get; set; }
        public string? CashRegisterName { get; set; }
        public string? RegisterTime { get; set; } // e.g. "Caja 02\n08:16"
        public string ObservedMetric { get; set; } = string.Empty; // e.g. "8.4%"
        public string ThresholdLimit { get; set; } = string.Empty; // e.g. "≤ 5.0%"
        public string? ObservationRatio { get; set; } // e.g. "4 de 48 (8.3%) · Umbral: 5%"
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, RESOLVED, DISMISSED
        public string? ReviewResult { get; set; } // Justificada, Sanción aplicada, Escalada a auditoría
        public string? SupervisorNotes { get; set; }
        public string? ResolvedBy { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public bool EvidenceAttached { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
