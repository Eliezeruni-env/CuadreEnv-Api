using System.Threading.Tasks;
using Onion.Domain;
using System.Collections.Generic;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface ICashMovementService
    {
        Task<IEnumerable<CashMovement>> GetAllAsync();
        Task<CashMovement> AddAsync(CashMovement movement);
    }
}
