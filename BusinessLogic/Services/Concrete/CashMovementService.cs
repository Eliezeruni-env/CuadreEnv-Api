using System.Collections.Generic;
using System.Threading.Tasks;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class CashMovementService : ICashMovementService
    {
        private readonly IUnitOfWork _uow;

        public CashMovementService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IEnumerable<CashMovement>> GetAllAsync()
        {
            return await _uow.CashMovements.ListAsync();
        }

        public async Task<CashMovement> AddAsync(CashMovement movement)
        {
            await _uow.CashMovements.AddAsync(movement);
            await _uow.SaveChangesAsync();
            return movement;
        }
    }
}
