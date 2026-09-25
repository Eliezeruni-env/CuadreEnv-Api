using System.Threading.Tasks;
using Onion.Domain;
using System.Collections.Generic;
using Onion.Common.Models.Pagination;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface ICashRegisterService
    {
        Task<CashRegister?> GetByIdAsync(int id);
        Task<IEnumerable<CashRegister>> GetAllAsync();
        Task<PagedList<CashRegister>> GetPagedAsync(int pageNumber, int pageSize);
        Task<CashRegister> OpenAsync(CashRegister register);
        Task PauseAsync(int id, string reason, int? userId);
        Task ResumeAsync(int id, int? userId);
        Task CloseAsync(int id, decimal closingAmount, string? breakdownJson, int? userId);
    }
}
