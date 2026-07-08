using System.Threading.Tasks;
using Onion.Domain;
using System.Collections.Generic;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface ICashRegisterService
    {
        Task<CashRegister?> GetByIdAsync(int id);
        Task<IEnumerable<CashRegister>> GetAllAsync();
        Task<CashRegister> OpenAsync(CashRegister register);
        Task CloseAsync(int id, decimal closingAmount);
    }
}
