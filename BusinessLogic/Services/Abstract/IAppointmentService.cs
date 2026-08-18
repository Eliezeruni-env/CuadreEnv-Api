using System.Collections.Generic;
using System.Threading.Tasks;
using Onion.BussinesLogic.Dtos;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IAppointmentService
    {
        Task<IEnumerable<AppointmentDto>> ListAsync();
        Task<AppointmentDto?> GetByIdAsync(int id);
        Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto);
        Task<AppointmentDto> UpdateAsync(UpdateAppointmentDto dto);
        Task DeleteAsync(int id);
    }
}
