using System;
using System.Collections.Generic;

namespace Onion.BussinesLogic.Dtos
{
    public class AuditAlertItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string RegisterTime { get; set; } = string.Empty;
        public string ObservedMetric { get; set; } = string.Empty;
        public string ThresholdLimit { get; set; } = string.Empty;
        public string Severity { get; set; } = "Moderada";
        public string SeverityCode { get; set; } = "moderate";
        public string? ObservationRatio { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public string? SupervisorNotes { get; set; }
        public string? ReviewResult { get; set; }
        public bool EvidenceAttached { get; set; }
    }

    public class CashierAuditScoreDto
    {
        public int CashierId { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public int TotalSales { get; set; }
        public decimal CancellationRate { get; set; }
        public int NoSaleDrawers { get; set; }
        public string Status { get; set; } = "Sin alerta"; // 'Por revisar' | 'Sin alerta'
        public decimal AccumulatedDiscrepancyDop { get; set; }
    }

    public class CreateIncidentRequest
    {
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = "Moderada";
        public string SeverityCode { get; set; } = "moderate";
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public int? CashierId { get; set; }
        public string? CashierName { get; set; }
        public int? CashRegisterId { get; set; }
        public string? CashRegisterName { get; set; }
        public string? RegisterTime { get; set; }
        public string ObservedMetric { get; set; } = string.Empty;
        public string ThresholdLimit { get; set; } = string.Empty;
        public string? ObservationRatio { get; set; }
    }

    public class ResolveIncidentRequest
    {
        public string ReviewResult { get; set; } = "Justificada"; // Justificada, Sanción aplicada, Escalada a auditoría
        public string SupervisorNotes { get; set; } = string.Empty;
        public bool EvidenceAttached { get; set; }
    }

    public class DismissIncidentRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
