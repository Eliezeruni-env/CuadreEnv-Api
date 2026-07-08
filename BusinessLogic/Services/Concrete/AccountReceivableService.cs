using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Concrete;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain.Finance;
using Onion.Common.Exceptions;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class AccountReceivableService : IAccountReceivableService
    {
        private readonly IUnitOfWork _uow;

        public AccountReceivableService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<AccountReceivable> CreateFromSaleAsync(Onion.Domain.Sale sale)
        {
            if (sale == null) throw new ArgumentNullException(nameof(sale));
            if (sale.Status == Onion.Domain.SaleStatus.PAID) throw new CustomException(new Onion.Common.Models.Error { Code = "NO_AR", Message = "Sale already paid", Language = "EN" });

            var ar = new AccountReceivable
            {
                CompanyId = sale.CompanyId,
                CustomerId = sale.CustomerId,
                SaleId = sale.Id,
                TotalAmount = sale.Total,
                PaidAmount = sale.PaidAmount,
                DueDate = sale.DueDate ?? sale.Date.AddDays(30),
                Status = sale.PaidAmount >= sale.Total ? ReceivableStatus.Paid : ReceivableStatus.Open
            };

            await _uow.AccountReceivables.AddAsync(ar);
            await _uow.SaveChangesAsync();
            return ar;
        }

        public async Task RegisterPaymentAsync(int receivableId, decimal amount)
        {
            var ar = await _uow.AccountReceivables.GetByIdAsync(receivableId) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "AccountReceivable not found", Language = "EN" });
            if (amount <= 0) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_AMOUNT", Message = "Amount must be greater than zero", Language = "EN" });

            ar.PaidAmount += amount;
            if (ar.PaidAmount >= ar.TotalAmount) ar.Status = ReceivableStatus.Paid;
            _uow.AccountReceivables.Update(ar);
            await _uow.SaveChangesAsync();
        }

        public async Task<IEnumerable<AccountReceivable>> GetOverdueAsync()
        {
            var list = await _uow.AccountReceivables.ListAsync();
            var now = DateTime.UtcNow;
            return list.Where(a => a.DueDate < now && a.Balance > 0m);
        }

        public async Task<IEnumerable<AccountReceivable>> GetDueSoonAsync(int days)
        {
            if (days <= 0) days = 7;
            var list = await _uow.AccountReceivables.ListAsync();
            var now = DateTime.UtcNow;
            var until = now.AddDays(days);
            return list.Where(a => a.DueDate >= now && a.DueDate <= until && a.Balance > 0m);
        }

        public async Task<IEnumerable<AccountReceivable>> GetByCustomerAsync(int customerId)
        {
            return await _uow.AccountReceivables.FindAsync(a => a.CustomerId == customerId);
        }
    }
}
