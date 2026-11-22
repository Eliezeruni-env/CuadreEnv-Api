using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Onion.BussinesLogic.Dtos;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IFraudGuardianService
    {
        Task<IEnumerable<AuditAlertItemDto>> GetAlertsAsync(string? tab, CancellationToken ct = default);
        Task<IEnumerable<CashierAuditScoreDto>> GetCashierScoresAsync(CancellationToken ct = default);
        Task<AuditAlertItemDto> CreateIncidentAsync(CreateIncidentRequest request, CancellationToken ct = default);
        Task<AuditAlertItemDto> ResolveIncidentAsync(string id, ResolveIncidentRequest request, CancellationToken ct = default);
        Task<AuditAlertItemDto> DismissIncidentAsync(string id, DismissIncidentRequest request, CancellationToken ct = default);
    }
}
