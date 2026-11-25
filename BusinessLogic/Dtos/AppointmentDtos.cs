using System;
using System.Collections.Generic;

namespace Onion.BussinesLogic.Dtos
{
    // Legacy DTOs preserved for backward compatibility
    public record AppointmentDto(
        int Id,
        DateTime StartAt,
        DateTime EndAt,
        int? CustomerId,
        int? ServiceId,
        int? ResourceId,
        string? Notes,
        string Status);

    public record CreateAppointmentDto(
        DateTime StartAt,
        DateTime EndAt,
        int? CustomerId,
        int? ServiceId,
        int? ResourceId,
        string? Notes);

    public record UpdateAppointmentDto(
        int Id,
        DateTime StartAt,
        DateTime EndAt,
        int? CustomerId,
        int? ServiceId,
        int? ResourceId,
        string? Notes,
        string Status);

    public record ResourceDto(int Id, string Name, string? Type, bool IsActive);

    public record AvailabilityDto(int Id, int ResourceId, DateTime StartAt, DateTime EndAt, bool IsBlocked);

    // Rich CuadreEnv Agenda DTOs
    public record AvailableSlotDto(
        DateTime StartAt,
        DateTime EndAt,
        bool IsAvailable,
        int? ProfessionalId,
        string? ProfessionalName);

    public record DayAvailabilityDto(
        DateTime Date,
        List<AvailableSlotDto> Slots);

    public record GetAvailabilityQuery(
        int AppointmentServiceId,
        int? ProfessionalId,
        DateTime Date);

    public record CreatePublicReservationDto(
        int AppointmentServiceId,
        int? ProfessionalId,
        DateTime StartAt,
        string CustomerName,
        string CustomerPhone,
        string? CustomerEmail,
        string? Notes);

    public record ReservationResultDto(
        int AppointmentId,
        string BookingCode,
        string ManagementToken,
        DateTime StartAt,
        DateTime EndAt,
        string Status,
        string BillingStatus,
        int ProfessionalId,
        string ProfessionalName,
        string ServiceName,
        decimal Price,
        int DurationMinutes,
        string? Notes);

    public record AppointmentPublicStatusDto(
        int AppointmentId,
        string BookingCode,
        DateTime StartAt,
        DateTime EndAt,
        string Status,
        string BillingStatus,
        string CustomerName,
        string CustomerPhone,
        string ProfessionalName,
        string ServiceName,
        decimal Price,
        int DurationMinutes,
        string? CancellationReason);

    public record RescheduleRequestDto(
        DateTime NewStartAt,
        string? Reason);

    public record CancelAppointmentDto(
        string Reason);

    public record AppointmentFullDetailDto(
        int Id,
        string? BookingCode,
        DateTime StartAt,
        DateTime EndAt,
        string Status,
        string BillingStatus,
        string Origin,
        int? CustomerId,
        string CustomerName,
        string CustomerPhone,
        string? CustomerEmail,
        int? AppointmentServiceId,
        string? ServiceName,
        decimal Price,
        int DurationMinutes,
        int? ProfessionalId,
        string? ProfessionalName,
        int? SaleId,
        DateTime? ExpiresAt,
        string? Notes,
        string? CancellationReason,
        DateTime CreationDate);

    public record AppointmentSettingsDto(
        string TimeZone,
        string ConfirmationMode,
        int DefaultDurationMinutes,
        int MinLeadTimeHours,
        int MaxFutureBookingDays,
        int HoldSlotTimeoutMinutes,
        int MinCancelLeadTimeHours,
        int SlotIntervalMinutes,
        bool SendReminder24h,
        bool SendReminder1h,
        bool ChannelWhatsApp,
        bool ChannelEmail);

    public record BookingPageSettingsDto(
        string Slug,
        string BusinessName,
        string? LogoUrl,
        string? CoverImageUrl,
        string BrandColorHex,
        string? Description,
        string? Address,
        string? WhatsAppNumber,
        bool IsPublicActive);

    public record AppointmentProfessionalDto(
        int Id,
        int? UserId,
        string DisplayName,
        string? SpecialtyTitle,
        bool IsOwner,
        bool IsActive,
        int DisplayOrder,
        List<int> AssignedServiceIds);

    public record AppointmentServiceConfigDto(
        int Id,
        int ProductId,
        string ProductName,
        decimal Price,
        int DurationMinutes,
        int BufferTimeMinutes,
        bool IsPubliclyBookable,
        int DisplayOrder,
        string? ColorHex,
        bool IsActive,
        List<int> AssignedProfessionalIds);

    // Dashboard DTOs
    public record AgendaDashboardDto(
        int CitasHoy,
        int Pendientes,
        int Confirmadas,
        decimal IngresosFacturados,
        List<AgendaTodayTimelineItemDto> CitasDeHoy,
        List<AgendaPendingRequestItemDto> SolicitudesPendientes,
        List<AgendaWeeklyOccupancyDayDto> OcupacionSemanal,
        List<AgendaTodayTimelineItemDto> ProximasCitas);

    public record AgendaTodayTimelineItemDto(
        int Id,
        string TimeFormatted,
        DateTime StartAt,
        DateTime EndAt,
        string CustomerName,
        string CustomerPhone,
        string ServiceName,
        string ProfessionalName,
        string Status,
        string BillingStatus,
        decimal Price);

    public record AgendaPendingRequestItemDto(
        int Id,
        string BookingCode,
        string CustomerName,
        string CustomerPhone,
        DateTime StartAt,
        string ServiceName,
        string ProfessionalName,
        decimal Price,
        DateTime? ExpiresAt);

    public record AgendaWeeklyOccupancyDayDto(
        string DayLabel, // e.g. "Lun 9"
        DateTime Date,
        int OccupancyPercentage,
        int TotalSlots,
        int BookedSlots);

    // Reports DTOs
    public record AgendaReportsDto(
        int TotalCitas,
        int TasaAsistenciaPercentage,
        int TasaOcupacionPercentage,
        decimal TotalFacturado,
        List<AgendaDaySeriesDto> CitasPorDia,
        List<AgendaStatusDistributionDto> DistribucionPorEstado,
        List<AgendaTopServiceItemDto> TopServicios,
        List<AgendaStaffPerformanceItemDto> DesempenoPersonal);

    public record AgendaDaySeriesDto(
        int DayNumber,
        DateTime Date,
        int Count);

    public record AgendaStatusDistributionDto(
        string Status,
        string Label,
        int Count,
        int Percentage);

    public record AgendaTopServiceItemDto(
        int Rank,
        string ServiceName,
        int CitasCount,
        decimal TotalFacturado);

    public record AgendaStaffPerformanceItemDto(
        int ProfessionalId,
        string ProfessionalName,
        int CitasCompletadas,
        int OcupacionPercentage);

    // CRM Clients DTOs
    public record AgendaClientsOverviewDto(
        int TotalClientes,
        int NuevosEsteMes,
        int Recurrentes,
        List<AgendaClientListItemDto> Clientes);

    public record AgendaClientListItemDto(
        int? CustomerId,
        string Name,
        string Phone,
        string? Email,
        DateTime? UltimaCita,
        DateTime? ProximaCita,
        int TotalCitas,
        string Estado,
        List<AgendaClientHistoryItemDto> Historial);

    public record AgendaClientHistoryItemDto(
        int AppointmentId,
        string BookingCode,
        DateTime Date,
        string ServiceName,
        string ProfessionalName,
        decimal Amount,
        string Status);

    // Upsert Inputs
    public record UpsertProfessionalDto(
        int? Id,
        int? UserId,
        string DisplayName,
        string? SpecialtyTitle,
        bool IsOwner,
        bool IsActive,
        int DisplayOrder,
        List<int>? AssignedServiceIds);

    public record WeeklyScheduleInputDto(
        DayOfWeek DayOfWeek,
        TimeSpan StartTime,
        TimeSpan EndTime,
        TimeSpan? BreakStartTime,
        TimeSpan? BreakEndTime,
        bool IsWorkingDay);

    public record AvailabilityExceptionInputDto(
        DateTime Date,
        TimeSpan? StartTime,
        TimeSpan? EndTime,
        bool IsAllDay,
        string? Reason);

    public record UpsertAppointmentServiceInputDto(
        int? Id,
        int ProductId,
        int DurationMinutes,
        int BufferTimeMinutes,
        bool IsPubliclyBookable,
        int DisplayOrder,
        string? ColorHex,
        bool IsActive,
        List<int>? AssignedProfessionalIds);

    public record CreateManualAppointmentDto(
        int AppointmentServiceId,
        int ProfessionalId,
        DateTime StartAt,
        int? CustomerId,
        string CustomerName,
        string CustomerPhone,
        string? CustomerEmail,
        string? Notes);

    // Public Portal DTOs
    public record PublicBusinessProfileDto(
        string Slug,
        string BusinessName,
        string? Description,
        string? Address,
        string? WhatsAppNumber,
        string? LogoUrl,
        string? CoverImageUrl,
        string BrandColorHex,
        string ConfirmationMode,
        int MinLeadTimeHours,
        int MaxFutureBookingDays,
        List<PublicServiceItemDto> Services,
        List<PublicProfessionalItemDto> Professionals);

    public record PublicServiceItemDto(
        int Id,
        int ProductId,
        string Name,
        decimal Price,
        int DurationMinutes,
        int BufferTimeMinutes,
        string? ColorHex,
        int DisplayOrder);

    public record PublicProfessionalItemDto(
        int Id,
        string DisplayName,
        string? SpecialtyTitle,
        int DisplayOrder,
        List<int> AssignedServiceIds);

    public record PublicCancelRequestDto(
        string BookingCode,
        string ManagementToken,
        string Reason);

    public record PublicRescheduleRequestDto(
        string BookingCode,
        string ManagementToken,
        DateTime NewStartAt,
        string? Reason);
}
