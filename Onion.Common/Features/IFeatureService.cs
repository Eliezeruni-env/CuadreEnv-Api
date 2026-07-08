using System.Threading.Tasks;

namespace Onion.Common.Features
{
    public interface IFeatureService
    {
        Task<bool> CompanyHasFeatureAsync(int companyId, string featureKey);
    }
}
