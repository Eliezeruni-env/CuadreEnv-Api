using System;
using System.Threading;

namespace Onion.DataAccess.Tenant
{
    // Ambient tenant provider that supports manual override from background jobs.
    public class AmbientTenantProvider
    {
        private static readonly AsyncLocal<int?> _ambient = new AsyncLocal<int?>();

        public int? GetCompanyId() => _ambient.Value;

        public void SetCompanyId(int? companyId) => _ambient.Value = companyId;

        public void Clear() => _ambient.Value = null;

        public bool HasOverride => _ambient.Value.HasValue;

        // Static accessor for Ambient value so other components (e.g., DbContext) can read it without DI
        public static int? CurrentCompanyId
        {
            get => _ambient.Value;
            set => _ambient.Value = value;
        }
    }
}
