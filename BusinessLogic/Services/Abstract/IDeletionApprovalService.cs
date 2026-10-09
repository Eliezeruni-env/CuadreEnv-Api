using Onion.Domain.Authorization;

namespace Onion.BussinesLogic.Services.Abstract;

public sealed record DeletionApprovalDto(int Id, int CompanyId, string EntityType, int EntityId, int RequestedByUserId, DateTime RequestedAt, string? Reason, string Status);

public interface IDeletionApprovalService
{
    Task<DeletionApprovalDto> RequestAsync(string entityType, int entityId, string? reason);
    Task<IReadOnlyCollection<DeletionApprovalDto>> GetPendingAsync();
    Task ApproveAsync(int id, string? notes);
    Task RejectAsync(int id, string? notes);
}
