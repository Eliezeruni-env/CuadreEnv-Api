using System;
using System.Linq;
using System.Threading.Tasks;
using Onion.Common.Authorization;
using Onion.Common.Features;
using Onion.Common.Services;
using Onion.DataAccess.Repositories.Concrete;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class FeatureService : IFeatureService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICompanyEntitlementService? _entitlementService;

        public FeatureService(IUnitOfWork uow, ICompanyEntitlementService? entitlementService = null)
        {
            _uow = uow;
            _entitlementService = entitlementService;
        }

        public async Task<bool> CompanyHasFeatureAsync(int companyId, string featureKey)
        {
            var normalizedKey = ModuleCodes.Normalize(featureKey) ?? featureKey;

            if (_entitlementService != null && companyId > 0)
            {
                try
                {
                    var entitlements = await _entitlementService.GetAsync(companyId);
                    if (entitlements != null && entitlements.Modules != null)
                    {
                        if (entitlements.Modules.Contains("*") ||
                            entitlements.Modules.Contains(featureKey, StringComparer.OrdinalIgnoreCase) ||
                            entitlements.Modules.Contains(normalizedKey, StringComparer.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    // Fallback to local subscription plan features
                }
            }

            var csList = await _uow.CompanySubscriptions.FindAsync(s => s.CompanyId == companyId);
            var cs = csList.FirstOrDefault();
            if (cs != null)
            {
                var plan = await _uow.SubscriptionPlans.GetByIdAsync(cs.SubscriptionPlanId);
                if (plan != null)
                {
                    var features = (plan.Features ?? string.Empty).Split(',', System.StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim());
                    if (features.Contains("*") ||
                        features.Contains(featureKey, StringComparer.OrdinalIgnoreCase) ||
                        features.Contains(normalizedKey, StringComparer.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            // For appointments / agenda module, permit access if company is valid
            if (string.Equals(normalizedKey, "appointments", StringComparison.OrdinalIgnoreCase))
            {
                if (companyId > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
