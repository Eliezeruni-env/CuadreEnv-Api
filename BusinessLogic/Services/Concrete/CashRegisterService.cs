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

        public CashRegisterService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<CashRegister?> GetByIdAsync(int id) => await _uow.CashRegisters.GetByIdAsync(id);

        public async Task<IEnumerable<CashRegister>> GetAllAsync() => await _uow.CashRegisters.ListAsync();

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
