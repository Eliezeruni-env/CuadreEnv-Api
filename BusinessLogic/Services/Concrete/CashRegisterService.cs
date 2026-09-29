using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
            if (register.OpeningAmount < 0) throw new InvalidOperationException("Opening amount cannot be negative");
            register.InitialAmount = register.OpeningAmount;
            register.OpenedAt = DateTime.UtcNow;
            register.IsOpen = true;
            register.Status = CashRegisterStatus.OPEN;
            await _uow.CashRegisters.AddAsync(register);
            await _uow.SaveChangesAsync();
            return register;
        }

        public async Task PauseAsync(int id, string reason, int? userId)
        {
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Pause reason is required", nameof(reason));
            var register = await _uow.CashRegisters.GetByIdAsync(id) ?? throw new InvalidOperationException("Cash register not found");
            if (register.Status != CashRegisterStatus.OPEN) throw new InvalidOperationException("Only an open cash register can be paused");
            register.Status = CashRegisterStatus.PAUSED;
            register.IsOpen = false;
            register.PausedAt = DateTime.UtcNow;
            register.PausedByUserId = userId;
            register.PauseReason = reason.Trim();
            await _uow.CashRegisterPauses.AddAsync(new CashRegisterPause { CashRegisterId = id, CompanyId = register.CompanyId, UserId = userId ?? 0, Reason = reason.Trim(), PausedAt = DateTime.UtcNow });
            _uow.CashRegisters.Update(register);
            await _uow.SaveChangesAsync();
        }

        public async Task ResumeAsync(int id, int? userId)
        {
            var register = await _uow.CashRegisters.GetByIdAsync(id) ?? throw new InvalidOperationException("Cash register not found");
            if (register.Status != CashRegisterStatus.PAUSED) throw new InvalidOperationException("Only a paused cash register can be resumed");
            register.Status = CashRegisterStatus.OPEN;
            register.IsOpen = true;
            var pause = (await _uow.CashRegisterPauses.FindAsync(p => p.CashRegisterId == id && p.ResumedAt == null)).OrderByDescending(p => p.PausedAt).FirstOrDefault();
            if (pause != null) pause.ResumedAt = DateTime.UtcNow;
            _uow.CashRegisters.Update(register);
            await _uow.SaveChangesAsync();
        }

        public async Task CloseAsync(int id, decimal closingAmount, string? breakdownJson, int? userId)
        {
            var existing = await _uow.CashRegisters.GetByIdAsync(id);
            if (existing == null) throw new InvalidOperationException("Cash register not found");
            if (existing.Status == CashRegisterStatus.CLOSED || existing.IsImmutable) throw new InvalidOperationException("Cash register is already closed");
            if (closingAmount < 0) throw new ArgumentException("Closing amount cannot be negative", nameof(closingAmount));
            var movements = (await _uow.CashMovements.FindAsync(m => m.CashRegisterId == id)).Sum(m => m.Amount);
            var expected = existing.InitialAmount + movements;
            existing.Close(Onion.Domain.Finance.CashDiscrepancy.Calculate(expected, closingAmount), userId, breakdownJson, DateTime.UtcNow);
            try
            {
                _uow.CashRegisters.Update(existing);
                await _uow.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException("Cash register was changed by another process; reload it before closing.");
            }
        }
    }
}
