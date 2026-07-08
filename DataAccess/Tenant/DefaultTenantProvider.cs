using System;

namespace Onion.DataAccess
{
    // Simple default tenant provider used when no tenant information is available.
    // Returns null so multi-tenant query filters that depend on a company id are effectively disabled.
    public class DefaultTenantProvider : ITenantProvider
    {
        public int? GetCompanyId()
        {
            return null;
        }
    }
}
