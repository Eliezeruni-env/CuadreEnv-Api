using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class CashRegisterService : ICashRegisterService
    {
        private readonly IUnitOfWork _uow;
        private readonly Onion.BussinesLogic.Services.Abstract.IPaginationService _paginationService;

        public CashRegisterService(IUnitOfWork uow, Onion.BussinesLogic.Services.Abstract.IPaginationService paginationService)
        {
            _uow = uow;
            _paginationService = paginationService;
        }

        public async Task<CashRegister?> GetByIdAsync(int id) => await _uow.CashRegisters.GetByIdAsync(id);

        public async Task<IEnumerable<CashRegister>> GetAllAsync() => await _uow.CashRegisters.ListAsync();

        public async Task<Onion.Common.Models.Pagination.PagedList<CashRegister>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var pn = Math.Max(1, pageNumber);
            var ps = Math.Clamp(pageSize, 1, 100);
            var list = (await _uow.CashRegisters.ListAsync()).AsQueryable();
            return await _paginationService.ToPagedListAsync(list, pn, ps);
        }

        public async Task<CashRegister> OpenAsync(CashRegister register)
        {
            register.OpenedAt = DateTime.UtcNow;
            register.IsOpen = true;
            await _uow.CashRegisters.AddAsync(register);
            await _uow.SaveChangesAsync();
            return register;
        }

        public async Task CloseAsync(int id, decimal closingAmount)
        {
            var existing = await _uow.CashRegisters.GetByIdAsync(id);
            if (existing == null) throw new InvalidOperationException("Cash register not found");
            existing.ClosingAmount = closingAmount;
            existing.ClosedAt = DateTime.UtcNow;
            existing.IsOpen = false;
            _uow.CashRegisters.Update(existing);
            await _uow.SaveChangesAsync();
        }
    }
}
