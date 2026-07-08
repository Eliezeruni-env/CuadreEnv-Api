using System.Threading.Tasks;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface ISubscriptionService
    {
        Task<Onion.Domain.Billing.CompanySubscription?> GetCompanySubscriptionAsync(int companyId);
        Task<bool> CanCreateUserAsync(int companyId);
        Task<bool> CanCreateWarehouseAsync(int companyId);
        Task<bool> CanCreateProductAsync(int companyId);
    }
}
