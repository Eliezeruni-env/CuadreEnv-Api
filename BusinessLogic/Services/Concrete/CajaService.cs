using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Common.Exceptions;
using Onion.Domain;
using Onion.BussinesLogic.Background;
using Onion.DataAccess.Repositories.Concrete;
using Onion.BussinesLogic.Background;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class CajaService : ICajaService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<CajaService> _logger;
        private readonly IBackgroundQueue _queue;

        public CajaService(IUnitOfWork uow, ILogger<CajaService> logger, IBackgroundQueue queue)
        {
            _uow = uow;
            _logger = logger;
            _queue = queue;
        }

        public async Task<Sale> CreateSaleAsync(Sale sale, string idempotencyKey, int? cashRegisterId = null)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("IdempotencyKey required", nameof(idempotencyKey));

            // Idempotency: check existing
            var existing = (await _uow.Sales.FindAsync(s => s.IdempotencyKey == idempotencyKey && !s.IsDeleted)).FirstOrDefault();
            if (existing != null) return existing;

            // compute subtotal etc
            decimal subtotal = 0m;
            foreach (var d in sale.Details) subtotal += d.Quantity * d.UnitPrice;
            sale.SubTotal = subtotal;
            sale.Tax = 0m; // tax calculation can be added
            sale.Total = sale.SubTotal + sale.Tax;
            sale.PaidAmount = sale.PaidAmount;
            sale.IdempotencyKey = idempotencyKey;
            sale.CashRegisterId = cashRegisterId;

            try
            {
                await _uow.Sales.AddAsync(sale);
                await _uow.SaveChangesAsync();

                // try synchronous post-processing (e.g., fiscal printer) - simulated here
                try
                {
                    // simulate integration; if fails, enqueue
                    // Integration code omitted - replace with real call
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Synchronous integration failed for sale {SaleId}, enqueueing fallback", sale.Id);
                    _queue.Enqueue(new BackgroundJob { Type = "ProcessSale", Payload = sale.Id.ToString() });
                }

                return sale;
            }
            catch (CustomException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create sale");
                // enqueue fallback
                _queue.Enqueue(new BackgroundJob { Type = "CreateSaleFallback", Payload = idempotencyKey });
                throw new CustomException(new Onion.Common.Models.Error { Code = "DB_ERROR", Message = "Failed to create sale, queued for retry", Language = "EN" });
            }
        }
    }
}
