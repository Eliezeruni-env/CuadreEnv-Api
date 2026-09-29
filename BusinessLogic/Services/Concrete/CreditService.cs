using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Abstract;
using Onion.DataAccess;
using Onion.Domain.Credits;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class CreditService : ICreditService
    {
        private readonly IRepository<Credit> _credits;
        private readonly IRepository<CreditPayment> _payments;
        private readonly IRepository<CreditStatusHistory> _history;
        private readonly OnionDbContext _db;
        private readonly Onion.BussinesLogic.Services.Abstract.IPaginationService _paginationService;

        public CreditService(IRepository<Credit> credits, IRepository<CreditPayment> payments, IRepository<CreditStatusHistory> history, OnionDbContext db, Onion.BussinesLogic.Services.Abstract.IPaginationService paginationService)
        {
            _credits = credits;
            _payments = payments;
            _history = history;
            _db = db;
            _paginationService = paginationService;
        }

        public async Task<CreditDto> CreateAsync(CreateCreditDto dto)
        {
            if (dto.TotalAmount <= 0) throw new ArgumentException("TotalAmount must be > 0");
            var e = new Credit
            {
                CustomerId = dto.CustomerId,
                TotalAmount = dto.TotalAmount,
                PaidAmount = 0,
                Balance = dto.TotalAmount,
                DueDate = dto.DueDate,
                Status = CreditStatus.PENDING,
                MinimumPaymentAmount = dto.MinimumPaymentAmount,
                PaymentFrequency = dto.PaymentFrequency
            };
            await _credits.AddAsync(e);
            await _db.SaveChangesAsync(CancellationToken.None);
            return MapToDto(e);
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _credits.GetByIdAsync(id);
            if (existing == null) return;
            _credits.Remove(existing);
            await _db.SaveChangesAsync(CancellationToken.None);
        }

        public async Task<CreditDto?> GetByIdAsync(int id)
        {
            var e = await _credits.GetByIdAsync(id);
            if (e == null) return null;
            return MapToDto(e);
        }

        public async Task<IEnumerable<CreditDto>> ListAsync()
        {
            var list = await _credits.ListAsync();
            return list.Select(MapToDto);
        }

        public async Task<Onion.Common.Models.Pagination.PagedList<CreditDto>> ListPagedAsync(int pageNumber, int pageSize)
        {
            var pn = Math.Max(1, pageNumber);
            var ps = Math.Max(1, Math.Min(pageSize, 100));
            var list = (await _credits.ListAsync()).Select(MapToDto).AsQueryable();
            return await _paginationService.ToPagedListAsync(list, pn, ps);
        }

        public async Task<CreditDto> UpdateAsync(UpdateCreditDto dto)
        {
            var existing = await _credits.GetByIdAsync(dto.Id);
            if (existing == null) throw new KeyNotFoundException("Credit not found");
            if (dto.TotalAmount.HasValue) existing.TotalAmount = dto.TotalAmount.Value;
            if (dto.DueDate.HasValue) existing.DueDate = dto.DueDate.Value;
            existing.MinimumPaymentAmount = dto.MinimumPaymentAmount ?? existing.MinimumPaymentAmount;
            existing.PaymentFrequency = dto.PaymentFrequency ?? existing.PaymentFrequency;
            existing.Balance = existing.TotalAmount - existing.PaidAmount;
            _credits.Update(existing);
            await _db.SaveChangesAsync(CancellationToken.None);
            return MapToDto(existing);
        }

        public async Task<CreditPaymentDto> AddPaymentAsync(int creditId, CreateCreditPaymentDto dto, int? userId = null)
        {
            var credit = await _credits.GetByIdAsync(creditId);
            if (credit == null) throw new KeyNotFoundException("Credit not found");
            if (credit.Status == CreditStatus.CANCELLED || credit.Status == CreditStatus.PAID) throw new InvalidOperationException("Cannot pay a closed credit");
            if (dto.Amount <= 0) throw new ArgumentException("Amount must be > 0");
            if (credit.MinimumPaymentAmount.HasValue && dto.Amount < credit.MinimumPaymentAmount.Value) throw new InvalidOperationException("Amount is less than minimum payment");
            if (dto.Amount > credit.Balance) throw new InvalidOperationException("Amount exceeds remaining balance");

            var p = new CreditPayment
            {
                CreditId = creditId,
                Amount = dto.Amount,
                PaidAt = DateTime.UtcNow,
                Notes = dto.Notes
            };
            await _payments.AddAsync(p);

            // update credit
            credit.PaidAmount += dto.Amount;
            credit.Balance = credit.TotalAmount - credit.PaidAmount;
            if (credit.Balance <= 0)
            {
                var old = credit.Status;
                credit.Status = CreditStatus.PAID;
                await _history.AddAsync(new CreditStatusHistory { CreditId = credit.Id, OldStatus = old.ToString(), NewStatus = credit.Status.ToString(), ChangedAt = DateTime.UtcNow, ChangedByUserId = userId ?? 0, CompanyId = _db.TenantCompanyId ?? credit.CompanyId });
            }
            else
            {
                if (credit.Status == CreditStatus.PENDING) credit.Status = CreditStatus.ACTIVE;
            }

            await _db.SaveChangesAsync(CancellationToken.None);
            return new CreditPaymentDto(p.Id, p.CreditId, p.Amount, p.PaidAt, p.Notes);
        }

        public async Task CheckOverdueAsync()
        {
            var now = DateTime.UtcNow.Date;
            var overdue = (await _credits.ListAsync()).Where(c => c.Status != CreditStatus.PAID && c.Status != CreditStatus.CANCELLED && c.DueDate.Date < now).ToList();
            foreach (var c in overdue)
            {
                var old = c.Status;
                c.Status = CreditStatus.OVERDUE;
                await _history.AddAsync(new CreditStatusHistory { CreditId = c.Id, OldStatus = old.ToString(), NewStatus = c.Status.ToString(), ChangedAt = DateTime.UtcNow, ChangedByUserId = 0, CompanyId = _db.TenantCompanyId ?? c.CompanyId });
                // business rule: if overdue beyond due date, cancel? for now mark overdue; additional cancellation may be scheduled
            }
            await _db.SaveChangesAsync(CancellationToken.None);
        }

        private static CreditDto MapToDto(Credit e)
        {
            return new CreditDto(e.Id, e.CustomerId, e.TotalAmount, e.PaidAmount, e.Balance, e.DueDate, e.Status.ToString(), e.MinimumPaymentAmount, e.PaymentFrequency);
        }
    }
}
