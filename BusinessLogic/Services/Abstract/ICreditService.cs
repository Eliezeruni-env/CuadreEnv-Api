using System.Collections.Generic;
using System.Threading.Tasks;
using Onion.BussinesLogic.Dtos;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface ICreditService
    {
        Task<IEnumerable<CreditDto>> ListAsync();
        Task<CreditDto?> GetByIdAsync(int id);
        Task<CreditDto> CreateAsync(CreateCreditDto dto);
        Task<CreditDto> UpdateAsync(UpdateCreditDto dto);
        Task DeleteAsync(int id);
        Task<CreditPaymentDto> AddPaymentAsync(int creditId, CreateCreditPaymentDto dto, int? userId = null);
        Task CheckOverdueAsync();
    }
}
