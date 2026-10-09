using Onion.BussinesLogic.Dtos;
using System.Threading.Tasks;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface ICompanySettingsService
    {
        Task<CompanySettingsEditableDto> GetSettingsAsync(int companyId);
        Task UpdateSettingsAsync(int companyId, UpdateCompanySettingsDto dto);
    }
}
