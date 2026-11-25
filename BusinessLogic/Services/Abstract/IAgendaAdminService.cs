using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Onion.BussinesLogic.Dtos;
using Onion.Domain.Appointments;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IAgendaAdminService
    {
        Task<AgendaDashboardDto> GetDashboardAsync(int companyId);
        Task<AgendaReportsDto> GetReportsAsync(int companyId, int? month = null, int? year = null);
        Task<AgendaClientsOverviewDto> GetClientsOverviewAsync(int companyId, string? search = null);

        Task<List<AppointmentServiceConfigDto>> ListServicesAsync(int companyId);
        Task<AppointmentServiceConfigDto> UpsertServiceAsync(int companyId, UpsertAppointmentServiceInputDto dto);

        Task<List<AppointmentProfessionalDto>> ListProfessionalsAsync(int companyId);
        Task<AppointmentProfessionalDto> UpsertProfessionalAsync(int companyId, UpsertProfessionalDto dto);
        Task<bool> UpdateWeeklyScheduleAsync(int companyId, int professionalId, List<WeeklyScheduleInputDto> schedule);
        Task<AppointmentAvailabilityException> AddExceptionAsync(int companyId, int professionalId, AvailabilityExceptionInputDto dto);
        Task<bool> DeleteExceptionAsync(int companyId, int exceptionId);

        Task<(AppointmentSettingsDto Settings, BookingPageSettingsDto BookingPage)> GetSettingsAsync(int companyId);
        Task<AppointmentSettingsDto> UpdateOperationalSettingsAsync(int companyId, AppointmentSettingsDto dto);
        Task<BookingPageSettingsDto> UpdateBookingPageSettingsAsync(int companyId, BookingPageSettingsDto dto);
        Task<PublicBusinessProfileDto?> GetPublicBusinessProfileAsync(string slug);

        Task<AppointmentFullDetailDto> CreateManualAppointmentAsync(int companyId, CreateManualAppointmentDto dto, string? userId = null);
    }
}
