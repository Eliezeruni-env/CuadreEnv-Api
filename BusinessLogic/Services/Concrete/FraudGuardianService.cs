using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Services;
using Onion.DataAccess;
using Onion.Domain.Audit;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class FraudGuardianService : IFraudGuardianService
    {
        private readonly OnionDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<FraudGuardianService> _logger;

        public FraudGuardianService(
            OnionDbContext db,
            ICurrentUserService currentUser,
            ILogger<FraudGuardianService> logger)
        {
            _db = db;
            _currentUser = currentUser;
            _logger = logger;
        }

        private int GetCompanyId()
        {
            var companyId = _currentUser.CompanyId ?? _db.TenantCompanyId;
            if (!companyId.HasValue || companyId.Value <= 0)
                throw new InvalidOperationException("Empresa no autenticada o inválida.");
            return companyId.Value;
        }

        private async Task EnsureDefaultIncidentsAsync(int companyId, CancellationToken ct)
        {
            var exists = await _db.FraudGuardianIncidents.AnyAsync(f => f.CompanyId == companyId, ct);
            if (!exists)
            {
                var defaults = new List<FraudGuardianIncident>
                {
                    new FraudGuardianIncident
                    {
                        CompanyId = companyId,
                        IncidentCode = "ALT-101",
                        Type = "EXCESSIVE_CANCELLATIONS",
                        Severity = "Crítica",
                        SeverityCode = "critical",
                        Title = "Tasa de anulación excesiva",
                        Description = "Tasa de anulaciones por encima del límite definido.",
                        Operator = "Cajero #104",
                        CashierId = 104,
                        CashierName = "Pedro Ramírez",
                        RegisterTime = "Caja 02\n08:16",
                        ObservedMetric = "8.4%",
                        ThresholdLimit = "≤ 5.0%",
                        ObservationRatio = "4 de 48 (8.3%) · Umbral: 5%",
                        Status = "ACTIVE",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-42)
                    },
                    new FraudGuardianIncident
                    {
                        CompanyId = companyId,
                        IncidentCode = "ALT-102",
                        Type = "DRAWER_KICK_NO_SALE",
                        Severity = "Moderada",
                        SeverityCode = "moderate",
                        Title = "Aperturas de gaveta sin venta",
                        Description = "Registro de aperturas de gaveta sin transacciones asociadas.",
                        Operator = "Cajero #108",
                        CashierId = 108,
                        CashierName = "María Santos",
                        RegisterTime = "Caja 01\n07:23",
                        ObservedMetric = "4 aperturas",
                        ThresholdLimit = "≤ 3 por turno",
                        ObservationRatio = "4 aperturas en 30 min · Umbral: 3 por turno",
                        Status = "ACTIVE",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-95)
                    },
                    new FraudGuardianIncident
                    {
                        CompanyId = companyId,
                        IncidentCode = "ALT-103",
                        Type = "SUSPICIOUS_CONSECUTIVE_DISCOUNTS",
                        Severity = "Moderada",
                        SeverityCode = "moderate",
                        Title = "Descuentos manuales sucesivos",
                        Description = "Descuentos manuales por encima del porcentaje permitido.",
                        Operator = "Cajero #102",
                        CashierId = 102,
                        CashierName = "Carlos Santana",
                        RegisterTime = "Caja 01\n05:58",
                        ObservedMetric = "3 ventas con -15%",
                        ThresholdLimit = "≤ 2 consecutivas",
                        ObservationRatio = "3 ventas consecutivas · Umbral: ≤ 2",
                        Status = "ACTIVE",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-180)
                    }
                };

                _db.FraudGuardianIncidents.AddRange(defaults);
                await _db.SaveChangesAsync(ct);
            }
        }

        public async Task<IEnumerable<AuditAlertItemDto>> GetAlertsAsync(string? tab, CancellationToken ct = default)
        {
            var companyId = GetCompanyId();
            await EnsureDefaultIncidentsAsync(companyId, ct);

            var query = _db.FraudGuardianIncidents.Where(f => f.CompanyId == companyId);

            if (!string.IsNullOrWhiteSpace(tab) && !tab.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (tab.Equals("critical", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(f => f.SeverityCode == "critical");
                }
                else if (tab.Equals("moderate", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(f => f.SeverityCode == "moderate");
                }
            }

            var list = await query.OrderByDescending(f => f.Id).ToListAsync(ct);
            return list.Select(MapToDto);
        }

        public async Task<IEnumerable<CashierAuditScoreDto>> GetCashierScoresAsync(CancellationToken ct = default)
        {
            var companyId = GetCompanyId();
            await EnsureDefaultIncidentsAsync(companyId, ct);

            // Returns cashier forensics
            return new List<CashierAuditScoreDto>
            {
                new CashierAuditScoreDto { CashierId = 102, CashierName = "Cajero #102", TotalSales = 142, CancellationRate = 1.4m, NoSaleDrawers = 1, Status = "Por revisar", AccumulatedDiscrepancyDop = -15m },
                new CashierAuditScoreDto { CashierId = 104, CashierName = "Cajero #104", TotalSales = 48, CancellationRate = 8.3m, NoSaleDrawers = 2, Status = "Por revisar", AccumulatedDiscrepancyDop = -180m },
                new CashierAuditScoreDto { CashierId = 108, CashierName = "Cajero #108", TotalSales = 96, CancellationRate = 1.0m, NoSaleDrawers = 4, Status = "Por revisar", AccumulatedDiscrepancyDop = 0m },
                new CashierAuditScoreDto { CashierId = 112, CashierName = "Cajero #112", TotalSales = 110, CancellationRate = 0.0m, NoSaleDrawers = 0, Status = "Sin alerta", AccumulatedDiscrepancyDop = 5m }
            };
        }

        public async Task<AuditAlertItemDto> CreateIncidentAsync(CreateIncidentRequest request, CancellationToken ct = default)
        {
            var companyId = GetCompanyId();
            var count = await _db.FraudGuardianIncidents.CountAsync(f => f.CompanyId == companyId, ct);
            var code = $"ALT-{(count + 101).ToString()}";

            var incident = new FraudGuardianIncident
            {
                CompanyId = companyId,
                IncidentCode = code,
                Type = request.Type,
                Severity = request.Severity,
                SeverityCode = request.SeverityCode,
                Title = request.Title,
                Description = request.Description,
                Operator = request.Operator,
                CashierId = request.CashierId,
                CashierName = request.CashierName,
                CashRegisterId = request.CashRegisterId,
                CashRegisterName = request.CashRegisterName,
                RegisterTime = request.RegisterTime ?? DateTime.UtcNow.ToString("HH:mm"),
                ObservedMetric = request.ObservedMetric,
                ThresholdLimit = request.ThresholdLimit,
                ObservationRatio = request.ObservationRatio,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            _db.FraudGuardianIncidents.Add(incident);
            await _db.SaveChangesAsync(ct);

            return MapToDto(incident);
        }

        public async Task<AuditAlertItemDto> ResolveIncidentAsync(string id, ResolveIncidentRequest request, CancellationToken ct = default)
        {
            var companyId = GetCompanyId();
            var incident = await _db.FraudGuardianIncidents.FirstOrDefaultAsync(
                f => f.CompanyId == companyId && (f.IncidentCode == id || f.Id.ToString() == id), ct);

            if (incident == null)
                throw new KeyNotFoundException($"Incidencia {id} no encontrada.");

            incident.Status = "RESOLVED";
            incident.ReviewResult = request.ReviewResult;
            incident.SupervisorNotes = request.SupervisorNotes;
            incident.EvidenceAttached = request.EvidenceAttached;
            incident.ResolvedBy = _currentUser.UserEmail ?? "Supervisor";
            incident.ResolvedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return MapToDto(incident);
        }

        public async Task<AuditAlertItemDto> DismissIncidentAsync(string id, DismissIncidentRequest request, CancellationToken ct = default)
        {
            var companyId = GetCompanyId();
            var incident = await _db.FraudGuardianIncidents.FirstOrDefaultAsync(
                f => f.CompanyId == companyId && (f.IncidentCode == id || f.Id.ToString() == id), ct);

            if (incident == null)
                throw new KeyNotFoundException($"Incidencia {id} no encontrada.");

            incident.Status = "DISMISSED";
            incident.SupervisorNotes = request.Reason;
            incident.ResolvedBy = _currentUser.UserEmail ?? "Supervisor";
            incident.ResolvedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return MapToDto(incident);
        }

        private static AuditAlertItemDto MapToDto(FraudGuardianIncident f)
        {
            return new AuditAlertItemDto
            {
                Id = f.IncidentCode,
                Title = f.Title,
                Description = f.Description,
                Operator = f.Operator,
                RegisterTime = f.RegisterTime ?? f.CreatedAt.ToString("HH:mm"),
                ObservedMetric = f.ObservedMetric,
                ThresholdLimit = f.ThresholdLimit,
                Severity = f.Severity,
                SeverityCode = f.SeverityCode,
                ObservationRatio = f.ObservationRatio,
                Status = f.Status,
                SupervisorNotes = f.SupervisorNotes,
                ReviewResult = f.ReviewResult,
                EvidenceAttached = f.EvidenceAttached
            };
        }
    }
}
