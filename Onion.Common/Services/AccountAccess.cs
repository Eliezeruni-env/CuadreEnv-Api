namespace Onion.Common.Services;

public sealed record AccountAccessResult(bool IsActive, bool IsDeleted);

public sealed record CompanyEntitlements(
    IReadOnlySet<string> Modules,
    IReadOnlySet<string> Projects);

public interface ICompanyEntitlementService
{
    Task<CompanyEntitlements> GetAsync(int companyId, CancellationToken cancellationToken = default);
    void Invalidate(int companyId);
}
