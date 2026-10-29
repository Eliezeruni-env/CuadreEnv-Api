using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Exceptions;
using Onion.DataAccess;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class CashRegisterService : ICashRegisterService
    {
        private readonly IUnitOfWork _uow;
        private readonly Onion.BussinesLogic.Services.Abstract.IPaginationService _paginationService;
        private readonly OnionDbContext _db;
        private readonly ICashSessionService _cashSessionService;

        public CashRegisterService(IUnitOfWork uow, Onion.BussinesLogic.Services.Abstract.IPaginationService paginationService, OnionDbContext db, ICashSessionService cashSessionService)
        {
            _uow = uow;
            _paginationService = paginationService;
            _db = db;
            _cashSessionService = cashSessionService;
        }

        public async Task<CashRegister?> GetByIdAsync(int id) => await _uow.CashRegisters.GetByIdAsync(id);

        public async Task<IEnumerable<CashRegister>> GetAllAsync() => await _uow.CashRegisters.ListAsync();

        public async Task<Onion.Common.Models.Pagination.PagedList<CashRegister>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var pn = Math.Max(1, pageNumber);
            var ps = Math.Clamp(pageSize, 1, 100);
            return await _uow.CashRegisters.GetPagedAsync(pn, ps);
        }

        public async Task<CashRegister> OpenAsync(CashRegister register)
        {
            if (register.OpeningAmount < 0) throw new InvalidOperationException("Opening amount cannot be negative");
            if (register.CompanyId <= 0) throw new InvalidOperationException("Company is required");
            if (register.OpenedByUserId.HasValue)
            {
                var existing = (await _uow.CashRegisters.FindAsync(x => x.CompanyId == register.CompanyId && x.OpenedByUserId == register.OpenedByUserId && x.Status != CashRegisterStatus.CLOSED)).FirstOrDefault();
                if (existing != null) throw new InvalidOperationException("The cashier already has an open cash register.");
            }
            register.InitialAmount = register.OpeningAmount;
            register.OpenedAt = DateTime.UtcNow;
            register.IsOpen = true;
            register.Status = CashRegisterStatus.OPEN;
            await _uow.CashRegisters.AddAsync(register);
            await _uow.SaveChangesAsync();

            // Synchronize: Ensure an active CashSession exists for this CashRegister
            var existingSession = (await _uow.CashSessions.FindAsync(s => s.CashRegisterId == register.Id && s.Status == Onion.Domain.Finance.CashSessionStatus.Open && s.CompanyId == register.CompanyId)).FirstOrDefault();
            if (existingSession == null)
            {
                var newSession = new Onion.Domain.Finance.CashSession
                {
                    CompanyId = register.CompanyId,
                    CashRegisterId = register.Id,
                    CashierUserId = register.OpenedByUserId ?? 1,
                    OpeningAmount = register.OpeningAmount,
                    OpenedAt = register.OpenedAt
                };
                await _uow.CashSessions.AddAsync(newSession);
                await _uow.SaveChangesAsync();
            }

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

        public async Task<CashRegisterCloseResult> CloseAsync(int id, CloseCashSessionRequest request, int? userId)
        {
            if (request.ActualAmount < 0) throw new ArgumentException("Closing amount cannot be negative", nameof(request.ActualAmount));

            var activeSession = await _db.CashSessions.SingleOrDefaultAsync(x => x.CashRegisterId == id && x.Status == Onion.Domain.Finance.CashSessionStatus.Open);
            if (activeSession != null)
            {
                await _cashSessionService.CloseAsync(activeSession.CompanyId, activeSession.Id, request, userId ?? 0);
                var closedSession = await _db.CashSessions.AsNoTracking().SingleAsync(x => x.Id == activeSession.Id);
                var sessionDifference = closedSession.Difference ?? 0m;
                return new CashRegisterCloseResult(
                    closedSession.ExpectedAmount ?? 0m,
                    closedSession.ActualAmount ?? 0m,
                    sessionDifference,
                    sessionDifference == 0m ? "EXACT" : sessionDifference < 0m ? "SHORTAGE" : "SURPLUS",
                    sessionDifference == 0m,
                    Math.Abs(sessionDifference) > 50m && string.IsNullOrWhiteSpace(request.SupervisorPin));
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
            try
            {
                var existing = await _db.CashRegisters.SingleOrDefaultAsync(x => x.Id == id);
                if (existing == null) throw new InvalidOperationException("Cash register not found");
                if (existing.Status == CashRegisterStatus.CLOSED || existing.IsImmutable)
                {
                    return new CashRegisterCloseResult(existing.InitialAmount, request.ActualAmount, 0m, "EXACT", true, false);
                }

                var movements = await _db.CashMovements
                    .Where(m => m.CashRegisterId == id)
                    .SumAsync(m => (decimal?)m.Amount) ?? 0m;
                var expected = existing.InitialAmount + movements;
                var discrepancy = Onion.Domain.Finance.CashDiscrepancy.Calculate(expected, request.ActualAmount);
                existing.Close(discrepancy, userId, request.BreakdownJson ?? "{}", DateTime.UtcNow);

                if (Math.Abs(discrepancy.DifferenceAmount) > 0.01m &&
                    (string.IsNullOrWhiteSpace(request.Notes) || request.Notes.Trim().Length < 10))
                    throw new CashSessionException("CASH_CLOSE_JUSTIFICATION_REQUIRED", "Debe proporcionar una justificación detallada del descuadre (mínimo 10 caracteres).");

                _uow.CashRegisters.Update(existing);
                await _uow.SaveChangesAsync();
                await transaction.CommitAsync();
                return new CashRegisterCloseResult(
                    discrepancy.ExpectedAmount,
                    discrepancy.ActualAmount,
                    discrepancy.DifferenceAmount,
                    discrepancy.DifferenceAmount == 0m ? "EXACT" : discrepancy.DifferenceAmount < 0m ? "SHORTAGE" : "SURPLUS",
                    discrepancy.DifferenceAmount == 0m,
                    Math.Abs(discrepancy.DifferenceAmount) > 50m && string.IsNullOrWhiteSpace(request.SupervisorPin));
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException("Cash register was changed by another process; reload it before closing.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
