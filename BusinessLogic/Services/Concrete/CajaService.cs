using System;
using System.Threading.Tasks;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class CajaService : ICajaService
    {
        private readonly ISaleService _saleService;

        public CajaService(ISaleService saleService)
        {
            _saleService = saleService;
        }

        public async Task<Sale> CreateSaleAsync(Sale sale, string idempotencyKey, int? cashRegisterId = null)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("IdempotencyKey required", nameof(idempotencyKey));
            sale.IdempotencyKey = idempotencyKey.Trim();
            sale.CashRegisterId = cashRegisterId;
            return await _saleService.CreateAsync(sale);
        }
    }
}
