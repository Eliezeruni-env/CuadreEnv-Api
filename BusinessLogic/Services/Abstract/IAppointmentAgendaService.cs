using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Onion.BussinesLogic.Dtos;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IAppointmentAgendaService
    {
        // Public booking
        Task<ReservationResultDto> CreatePublicReservationAsync(string slug, CreatePublicReservationDto dto);
        Task<AppointmentPublicStatusDto?> GetPublicAppointmentStatusAsync(string slug, string bookingCode, string managementToken);
        Task<bool> CancelPublicAppointmentAsync(string slug, string bookingCode, string managementToken, string reason);
        Task<bool> RequestPublicRescheduleAsync(string slug, string bookingCode, string managementToken, RescheduleRequestDto dto);

        // State Machine Transitions
        Task<AppointmentFullDetailDto> ConfirmAppointmentAsync(int companyId, int appointmentId, string? userId = null);
        Task<AppointmentFullDetailDto> RejectAppointmentAsync(int companyId, int appointmentId, string reason, string? userId = null);
        Task<AppointmentFullDetailDto> StartAppointmentAsync(int companyId, int appointmentId, string? userId = null);
        Task<AppointmentFullDetailDto> CompleteAppointmentAsync(int companyId, int appointmentId, string? userId = null);
        Task<AppointmentFullDetailDto> MarkNoShowAsync(int companyId, int appointmentId, string? userId = null);
        Task<AppointmentFullDetailDto> CancelAdminAppointmentAsync(int companyId, int appointmentId, string reason, string? userId = null);
        Task<AppointmentFullDetailDto> RescheduleAppointmentAdminAsync(int companyId, int appointmentId, DateTime newStartAt, string? userId = null);

        // POS Invoicing Integration (Idempotent 1-to-1)
        Task<int> InvoiceAppointmentAsync(int companyId, int appointmentId, int cashSessionId, int? customerId, string? invoiceFolio);

        // Queries
        Task<AppointmentFullDetailDto?> GetAppointmentByIdAsync(int companyId, int appointmentId);
        Task<IEnumerable<AppointmentFullDetailDto>> ListAppointmentsAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null, string? status = null, int? professionalId = null);
    }
}
