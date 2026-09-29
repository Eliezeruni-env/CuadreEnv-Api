using System.Threading.Tasks;
using Onion.Domain;
using System.Collections.Generic;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface ICashMovementService
    {
        Task<IEnumerable<CashMovement>> GetAllAsync();
        Task<Onion.Common.Models.Pagination.PagedList<CashMovement>> GetPagedAsync(int pageNumber, int pageSize);
        Task<CashMovement> AddAsync(CashMovement movement);
    }
}
