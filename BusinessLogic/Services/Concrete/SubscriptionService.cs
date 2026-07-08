using System.Linq;
using System.Threading.Tasks;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Concrete;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IUnitOfWork _uow;

        public SubscriptionService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<Onion.Domain.Billing.CompanySubscription?> GetCompanySubscriptionAsync(int companyId)
        {
            var list = await _uow.CompanySubscriptions.FindAsync(s => s.CompanyId == companyId);
            return list.FirstOrDefault();
        }

        private async Task<Onion.Domain.Billing.SubscriptionPlan?> GetPlanForCompanyAsync(int companyId)
        {
            var cs = await GetCompanySubscriptionAsync(companyId);
            if (cs == null) return null;
            var plan = await _uow.SubscriptionPlans.GetByIdAsync(cs.SubscriptionPlanId);
            return plan;
        }

        public async Task<bool> CanCreateUserAsync(int companyId)
        {
            var plan = await GetPlanForCompanyAsync(companyId);
            int max = plan?.MaxUsers ?? 5; // default free
            var users = await _uow.Users.FindAsync(u => u.CompanyId == companyId);
            return users.Count() < max;
        }

        public async Task<bool> CanCreateWarehouseAsync(int companyId)
        {
            var plan = await GetPlanForCompanyAsync(companyId);
            int max = plan?.MaxWarehouses ?? 1;
            var wh = await _uow.Warehouses.FindAsync(w => w.CompanyId == companyId);
            return wh.Count() < max;
        }

        public async Task<bool> CanCreateProductAsync(int companyId)
        {
            var plan = await GetPlanForCompanyAsync(companyId);
            int max = plan?.MaxProducts ?? 100;
            var products = await _uow.Products.FindAsync(p => p.CompanyId == companyId);
            return products.Count() < max;
        }
    }
}
