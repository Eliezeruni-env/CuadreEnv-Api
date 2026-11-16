using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Onion.DataAccess.Tenant;

public sealed class TenantMembershipValidator : ITenantMembershipValidator
{
    private readonly OnionDbContext _db;

    public TenantMembershipValidator(OnionDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasActiveAccessAsync(int userId, int companyId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || companyId <= 0)
            return false;

        var user = await _db.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.Id == userId && x.Active && !x.IsDeleted)
            .Select(x => new { x.CompanyId })
            .SingleOrDefaultAsync(cancellationToken);

        if (user?.CompanyId == companyId && await IsActiveCompanyAsync(companyId, cancellationToken))
            return true;

        var activeCompany = await IsActiveCompanyAsync(companyId, cancellationToken);
        return activeCompany && await _db.UserCompanies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId &&
                          x.CompanyId == companyId &&
                          x.Active &&
                          !x.IsDeleted, cancellationToken);
    }

    private Task<bool> IsActiveCompanyAsync(int companyId, CancellationToken cancellationToken) =>
        _db.Companies.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(x => x.Id == companyId && x.Active && !x.IsDeleted, cancellationToken);
}
