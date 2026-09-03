using System.Threading.Tasks;
using Onion.BussinesLogic.Dtos;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface ICreditNoteService
    {
        Task<CreditNoteDto> CreateAsync(int companyId, CreateCreditNoteRequest request);
    }
}
