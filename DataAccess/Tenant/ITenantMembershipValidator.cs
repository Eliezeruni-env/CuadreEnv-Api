using System.Threading;
using System.Threading.Tasks;

namespace Onion.DataAccess.Tenant;

public interface ITenantMembershipValidator
{
    Task<bool> HasActiveAccessAsync(int userId, int companyId, CancellationToken cancellationToken = default);
}
