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
        // Create account receivable with optional payment plan
        public async Task<AccountReceivable> CreateWithPlanAsync(AccountReceivable ar, Onion.Domain.Finance.PaymentPlan? plan = null)
        {
            if (ar == null) throw new ArgumentNullException(nameof(ar));

            await _uow.AccountReceivables.AddAsync(ar);
            await _uow.SaveChangesAsync();

            if (plan != null)
            {
                plan.AccountReceivableId = ar.Id;

                var installments = new List<Onion.Domain.Finance.Installment>();
                var start = plan.StartDate == default ? ar.DueDate : plan.StartDate;
                for (int i = 1; i <= plan.TotalInstallments; i++)
                {
                    var due = start;
                    switch (plan.Frequency)
                    {
                        case Onion.Domain.Finance.PaymentFrequency.Daily: due = start.AddDays(i - 1); break;
                        case Onion.Domain.Finance.PaymentFrequency.Weekly: due = start.AddDays(7 * (i - 1)); break;
                        case Onion.Domain.Finance.PaymentFrequency.BiWeekly: due = start.AddDays(14 * (i - 1)); break;
                        case Onion.Domain.Finance.PaymentFrequency.Monthly: due = start.AddMonths(i - 1); break;
                    }

                    installments.Add(new Onion.Domain.Finance.Installment
                    {
                        Number = i,
                        DueDate = due,
                        Amount = plan.InstallmentAmount,
                        PaidAmount = 0m,
                        Status = Onion.Domain.Finance.InstallmentStatus.Expected
                    });
                }

                await _uow.PaymentPlans.AddAsync(plan);
                await _uow.SaveChangesAsync();

                // attach installments with PaymentPlanId now known
                foreach (var ins in installments)
                {
                    ins.PaymentPlanId = plan.Id;
                    await _uow.Installments.AddAsync(ins);
                }
            }

            await _uow.SaveChangesAsync();
            return ar;
        }

        public async Task<IEnumerable<Onion.Domain.Finance.Installment>> GetInstallmentsAsync(int paymentPlanId)
        {
            return await _uow.Installments.FindAsync(i => i.PaymentPlanId == paymentPlanId);
        }

        public async Task PayInstallmentAsync(int installmentId, decimal amount)
        {
            var ins = await _uow.Installments.GetByIdAsync(installmentId) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Installment not found", Language = "EN" });
            if (amount <= 0) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_AMOUNT", Message = "Amount must be greater than zero", Language = "EN" });

            ins.PaidAmount += amount;
            if (ins.PaidAmount >= ins.Amount) ins.Status = Onion.Domain.Finance.InstallmentStatus.Paid;
            else ins.Status = Onion.Domain.Finance.InstallmentStatus.Partial;

            _uow.Installments.Update(ins);
            await _uow.SaveChangesAsync();
        }
    }
}
