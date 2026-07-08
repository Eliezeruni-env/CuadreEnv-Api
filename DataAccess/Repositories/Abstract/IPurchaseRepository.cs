using System.Threading.Tasks;
using Onion.Domain;

namespace Onion.DataAccess.Repositories.Abstract
{
    public interface IPurchaseRepository : IRepository<Purchase>
    {
        Task<Purchase?> GetByIdWithDetailsAsync(int id);
    }
}
