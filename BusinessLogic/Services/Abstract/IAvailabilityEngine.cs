using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Onion.BussinesLogic.Dtos;
using Onion.Domain.Appointments;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IAvailabilityEngine
    {
        Task<DayAvailabilityDto> GetDayAvailabilityAsync(int companyId, int appointmentServiceId, int? professionalId, DateTime date);
        Task<AppointmentProfessional?> AutoAssignProfessionalAsync(int companyId, int appointmentServiceId, DateTime startAt, DateTime endAt);
        Task<bool> IsProfessionalAvailableAsync(int companyId, int professionalId, DateTime startAt, DateTime endAt, int? excludeAppointmentId = null);
        Task ExpireStalePendingAppointmentsAsync(int? companyId = null);
    }
}
