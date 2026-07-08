using System.Linq;
using System.Threading.Tasks;
using Onion.Common.Features;
using Onion.DataAccess.Repositories.Concrete;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class FeatureService : IFeatureService
    {
        private readonly IUnitOfWork _uow;

        public FeatureService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<bool> CompanyHasFeatureAsync(int companyId, string featureKey)
        {
            var csList = await _uow.CompanySubscriptions.FindAsync(s => s.CompanyId == companyId);
            var cs = csList.FirstOrDefault();
            if (cs == null) return false;
            var plan = await _uow.SubscriptionPlans.GetByIdAsync(cs.SubscriptionPlanId);
            if (plan == null) return false;
            var features = (plan.Features ?? string.Empty).Split(',', System.StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim());
            return features.Contains(featureKey);
        }
    }
}
