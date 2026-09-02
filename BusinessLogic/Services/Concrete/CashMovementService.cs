using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class CashMovementService : ICashMovementService
    {
        private readonly IUnitOfWork _uow;
        private readonly Onion.BussinesLogic.Services.Abstract.IPaginationService _paginationService;

        public CashMovementService(IUnitOfWork uow, Onion.BussinesLogic.Services.Abstract.IPaginationService paginationService)
        {
            _uow = uow;
            _paginationService = paginationService;
        }

        public async Task<IEnumerable<CashMovement>> GetAllAsync()
        {
            return await _uow.CashMovements.ListAsync();
        }

        public async Task<Onion.Common.Models.Pagination.PagedList<CashMovement>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var pn = System.Math.Max(1, pageNumber);
            var ps = System.Math.Clamp(pageSize, 1, 100);
            var list = (await _uow.CashMovements.ListAsync()).AsQueryable();
            return await _paginationService.ToPagedListAsync(list, pn, ps);
        }

        public async Task<CashMovement> AddAsync(CashMovement movement)
        {
            await _uow.CashMovements.AddAsync(movement);
            await _uow.SaveChangesAsync();
            return movement;
        }
    }
}
