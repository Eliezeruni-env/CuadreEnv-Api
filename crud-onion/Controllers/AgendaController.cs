using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Exceptions;
using Onion.Common.Features;
using Onion.Common.Models;
using Onion.DataAccess;

namespace Onion.Controllers
{
    [Route("v1/[controller]")]
    [Route("agenda")]
    [ApiController]
    [Authorize]
    public class AgendaController : ControllerBase
    {
        private readonly IAgendaAdminService _adminService;
        private readonly IAppointmentAgendaService _agendaService;
        private readonly IAvailabilityEngine _availability;
        private readonly ITenantProvider _tenantProvider;
        private readonly IFeatureService _featureService;

        public AgendaController(
            IAgendaAdminService adminService,
            IAppointmentAgendaService agendaService,
            IAvailabilityEngine availability,
            ITenantProvider tenantProvider,
            IFeatureService featureService)
        {
            _adminService = adminService;
            _agendaService = agendaService;
            _availability = availability;
            _tenantProvider = tenantProvider;
            _featureService = featureService;
        }

        private async Task<int> GetCompanyIdAsync()
        {
            var companyId = _tenantProvider.GetCompanyId();
            if (companyId is not > 0)
            {
                var claim = User.FindFirst("companyId")?.Value ?? User.FindFirst("CompanyId")?.Value;
                if (int.TryParse(claim, out var cid) && cid > 0) companyId = cid;
            }

            if (companyId is not > 0)
                throw new TenantRequiredException("Se requiere un contexto de empresa válido para acceder a la agenda.");

            var isAdmin = User.IsInRole("Admin") || User.IsInRole("Owner") || User.IsInRole("SuperAdmin") || User.IsInRole("GlobalAdministrator") || User.IsInRole("SuperUser");
            if (!isAdmin)
            {
                bool hasFeature = await _featureService.CompanyHasFeatureAsync(companyId.Value, "appointments");
                if (!hasFeature)
                    throw new CustomException(new Error { Code = "FEATURE_NOT_SUBSCRIBED", Message = "El módulo de Citas y Reservas no está incluido en su plan actual.", Language = "ES" });
            }

            return companyId.Value;
        }

        #region Dashboard & Overview

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            int companyId = await GetCompanyIdAsync();
            var data = await _adminService.GetDashboardAsync(companyId);
            return Ok(ApiResponse<AgendaDashboardDto>.Ok(data));
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReports([FromQuery] int? month, [FromQuery] int? year)
        {
            int companyId = await GetCompanyIdAsync();
            var data = await _adminService.GetReportsAsync(companyId, month, year);
            return Ok(ApiResponse<AgendaReportsDto>.Ok(data));
        }

        [HttpGet("clients")]
        public async Task<IActionResult> GetClients([FromQuery] string? search)
        {
            int companyId = await GetCompanyIdAsync();
            var data = await _adminService.GetClientsOverviewAsync(companyId, search);
            return Ok(ApiResponse<AgendaClientsOverviewDto>.Ok(data));
        }

        [HttpGet("clients/{customerId:int}/history")]
        public async Task<IActionResult> GetClientHistory(int customerId)
        {
            int companyId = await GetCompanyIdAsync();
            var list = await _agendaService.ListAppointmentsAsync(companyId, null, null, null, null);
            var filtered = System.Linq.Enumerable.Where(list, a => a.CustomerId == customerId);
            return Ok(ApiResponse<IEnumerable<AppointmentFullDetailDto>>.Ok(filtered));
        }

        #endregion

        #region Appointments Management

        [HttpGet("calendar")]
        public async Task<IActionResult> GetCalendar(
            [FromQuery] DateTime? start,
            [FromQuery] DateTime? end,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? professionalId)
        {
            int companyId = await GetCompanyIdAsync();
            var fromDate = start ?? from;
            var toDate = end ?? to;
            var list = await _agendaService.ListAppointmentsAsync(companyId, fromDate, toDate, null, professionalId);
            return Ok(ApiResponse<IEnumerable<AppointmentFullDetailDto>>.Ok(list));
        }

        [HttpGet("appointments")]
        public async Task<IActionResult> ListAppointments(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] DateTime? start,
            [FromQuery] DateTime? end,
            [FromQuery] string? status,
            [FromQuery] int? professionalId)
        {
            int companyId = await GetCompanyIdAsync();
            var fromDate = from ?? start;
            var toDate = to ?? end;
            var list = await _agendaService.ListAppointmentsAsync(companyId, fromDate, toDate, status, professionalId);
            return Ok(ApiResponse<IEnumerable<AppointmentFullDetailDto>>.Ok(list));
        }

        [HttpGet("appointments/{id:int}")]
        public async Task<IActionResult> GetAppointment(int id)
        {
            int companyId = await GetCompanyIdAsync();
            var item = await _agendaService.GetAppointmentByIdAsync(companyId, id);
            if (item == null) return NotFound(ApiResponse<object>.Fail("Cita no encontrada."));
            return Ok(ApiResponse<AppointmentFullDetailDto>.Ok(item));
        }

        [HttpPost("appointments")]
        public async Task<IActionResult> CreateManual([FromBody] CreateManualAppointmentDto dto)
        {
            int companyId = await GetCompanyIdAsync();
            var username = User.Identity?.Name ?? "Admin";
            var created = await _adminService.CreateManualAppointmentAsync(companyId, dto, username);
            return CreatedAtAction(nameof(GetAppointment), new { id = created.Id }, ApiResponse<AppointmentFullDetailDto>.Ok(created));
        }

        [HttpPut("appointments/{id:int}/status")]
        public async Task<IActionResult> UpdateAppointmentStatus(int id, [FromBody] UpdateAppointmentStatusDto dto)
        {
            int companyId = await GetCompanyIdAsync();
            var username = User.Identity?.Name ?? "Admin";
            var status = dto?.Status?.Trim();

            AppointmentFullDetailDto result;
            if (string.Equals(status, "Confirmed", StringComparison.OrdinalIgnoreCase))
            {
                result = await _agendaService.ConfirmAppointmentAsync(companyId, id, username);
            }
            else if (string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                result = await _agendaService.RejectAppointmentAsync(companyId, id, dto?.CancellationReason ?? "Rechazada", username);
            }
            else if (string.Equals(status, "InProgress", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "Started", StringComparison.OrdinalIgnoreCase))
            {
                result = await _agendaService.StartAppointmentAsync(companyId, id, username);
            }
            else if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                result = await _agendaService.CompleteAppointmentAsync(companyId, id, username);
            }
            else if (string.Equals(status, "NoShow", StringComparison.OrdinalIgnoreCase))
            {
                result = await _agendaService.MarkNoShowAsync(companyId, id, username);
            }
            else if (string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                result = await _agendaService.CancelAdminAppointmentAsync(companyId, id, dto?.CancellationReason ?? "Cancelada administrativamente", username);
            }
            else
            {
                var existing = await _agendaService.GetAppointmentByIdAsync(companyId, id);
                if (existing == null) return NotFound(ApiResponse<object>.Fail("Cita no encontrada."));
                result = existing;
            }

            return Ok(ApiResponse<AppointmentFullDetailDto>.Ok(result));
        }

        [HttpPost("appointments/{id:int}/confirm")]
        public async Task<IActionResult> Confirm(int id)
        {
            int companyId = await GetCompanyIdAsync();
            var username = User.Identity?.Name ?? "Admin";
            var result = await _agendaService.ConfirmAppointmentAsync(companyId, id, username);
            return Ok(ApiResponse<AppointmentFullDetailDto>.Ok(result));
        }

        [HttpPost("appointments/{id:int}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] CancelAppointmentDto dto)
        {
            int companyId = await GetCompanyIdAsync();
            var username = User.Identity?.Name ?? "Admin";
            var result = await _agendaService.RejectAppointmentAsync(companyId, id, dto?.Reason ?? "Rechazada", username);
            return Ok(ApiResponse<AppointmentFullDetailDto>.Ok(result));
        }

        [HttpPost("appointments/{id:int}/start")]
        public async Task<IActionResult> Start(int id)
        {
            int companyId = await GetCompanyIdAsync();
            var username = User.Identity?.Name ?? "Admin";
            var result = await _agendaService.StartAppointmentAsync(companyId, id, username);
            return Ok(ApiResponse<AppointmentFullDetailDto>.Ok(result));
        }

        [HttpPost("appointments/{id:int}/complete")]
        public async Task<IActionResult> Complete(int id)
        {
            int companyId = await GetCompanyIdAsync();
            var username = User.Identity?.Name ?? "Admin";
            var result = await _agendaService.CompleteAppointmentAsync(companyId, id, username);
            return Ok(ApiResponse<AppointmentFullDetailDto>.Ok(result));
        }

        [HttpPost("appointments/{id:int}/no-show")]
        public async Task<IActionResult> NoShow(int id)
        {
            int companyId = await GetCompanyIdAsync();
            var username = User.Identity?.Name ?? "Admin";
            var result = await _agendaService.MarkNoShowAsync(companyId, id, username);
            return Ok(ApiResponse<AppointmentFullDetailDto>.Ok(result));
        }

        [HttpPost("appointments/{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelAppointmentDto dto)
        {
            int companyId = await GetCompanyIdAsync();
            var username = User.Identity?.Name ?? "Admin";
            var result = await _agendaService.CancelAdminAppointmentAsync(companyId, id, dto?.Reason ?? "Cancelada administrativamente", username);
            return Ok(ApiResponse<AppointmentFullDetailDto>.Ok(result));
        }

        [HttpPost("appointments/{id:int}/reschedule")]
        public async Task<IActionResult> Reschedule(int id, [FromBody] RescheduleRequestDto dto)
        {
            int companyId = await GetCompanyIdAsync();
            var username = User.Identity?.Name ?? "Admin";
            var result = await _agendaService.RescheduleAppointmentAdminAsync(companyId, id, dto.NewStartAt, username);
            return Ok(ApiResponse<AppointmentFullDetailDto>.Ok(result));
        }

        [HttpPost("appointments/{id:int}/invoice")]
        public async Task<IActionResult> Invoice(int id, [FromBody] InvoiceAppointmentRequest request)
        {
            int companyId = await GetCompanyIdAsync();
            int cashSessionId = request?.CashSessionId ?? 0;
            int saleId = await _agendaService.InvoiceAppointmentAsync(companyId, id, cashSessionId, request?.CustomerId, request?.InvoiceFolio);
            return Ok(ApiResponse<object>.Ok(new { SaleId = saleId, InvoiceNumber = request?.InvoiceFolio ?? $"INV-{saleId}", Message = "Cita facturada exitosamente." }));
        }

        #endregion

        #region Services & Professionals

        [HttpGet("services")]
        public async Task<IActionResult> ListServices()
        {
            int companyId = await GetCompanyIdAsync();
            var list = await _adminService.ListServicesAsync(companyId);
            return Ok(ApiResponse<List<AppointmentServiceConfigDto>>.Ok(list));
        }

        [HttpPost("services")]
        public async Task<IActionResult> UpsertService([FromBody] UpsertAppointmentServiceInputDto dto)
        {
            try
            {
                int companyId = await GetCompanyIdAsync();
                var result = await _adminService.UpsertServiceAsync(companyId, dto);
                return Ok(ApiResponse<AppointmentServiceConfigDto>.Ok(result));
            }
            catch (CustomException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Error?.Message ?? ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail("Error al guardar servicio reservable: " + ex.Message));
            }
        }

        [HttpPut("services/{id:int}")]
        public async Task<IActionResult> UpdateService(int id, [FromBody] UpsertAppointmentServiceInputDto dto)
        {
            try
            {
                int companyId = await GetCompanyIdAsync();
                var input = dto with { Id = id };
                var result = await _adminService.UpsertServiceAsync(companyId, input);
                return Ok(ApiResponse<AppointmentServiceConfigDto>.Ok(result));
            }
            catch (CustomException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Error?.Message ?? ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail("Error al actualizar servicio reservable: " + ex.Message));
            }
        }

        [HttpGet("professionals")]
        public async Task<IActionResult> ListProfessionals()
        {
            int companyId = await GetCompanyIdAsync();
            var list = await _adminService.ListProfessionalsAsync(companyId);
            return Ok(ApiResponse<List<AppointmentProfessionalDto>>.Ok(list));
        }

        [HttpPost("professionals")]
        public async Task<IActionResult> UpsertProfessional([FromBody] UpsertProfessionalDto dto)
        {
            int companyId = await GetCompanyIdAsync();
            var result = await _adminService.UpsertProfessionalAsync(companyId, dto);
            return Ok(ApiResponse<AppointmentProfessionalDto>.Ok(result));
        }

        [HttpPut("professionals/{id:int}")]
        public async Task<IActionResult> UpdateProfessional(int id, [FromBody] UpsertProfessionalDto dto)
        {
            int companyId = await GetCompanyIdAsync();
            var input = dto with { Id = id };
            var result = await _adminService.UpsertProfessionalAsync(companyId, input);
            return Ok(ApiResponse<AppointmentProfessionalDto>.Ok(result));
        }

        [HttpPut("professionals/{id:int}/schedule")]
        public async Task<IActionResult> UpdateSchedule(int id, [FromBody] List<WeeklyScheduleInputDto> schedule)
        {
            int companyId = await GetCompanyIdAsync();
            await _adminService.UpdateWeeklyScheduleAsync(companyId, id, schedule);
            return Ok(ApiResponse<object>.Ok(new { Success = true }));
        }

        [HttpPost("professionals/{id:int}/exception")]
        [HttpPost("professionals/{id:int}/exceptions")]
        public async Task<IActionResult> AddException(int id, [FromBody] AvailabilityExceptionInputDto dto)
        {
            int companyId = await GetCompanyIdAsync();
            var created = await _adminService.AddExceptionAsync(companyId, id, dto);
            return Ok(ApiResponse<object>.Ok(created));
        }

        [HttpDelete("professionals/exceptions/{exceptionId:int}")]
        public async Task<IActionResult> DeleteException(int exceptionId)
        {
            int companyId = await GetCompanyIdAsync();
            bool deleted = await _adminService.DeleteExceptionAsync(companyId, exceptionId);
            return Ok(ApiResponse<object>.Ok(new { Success = deleted }));
        }

        #endregion

        #region Operational Settings & Booking Page

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            int companyId = await GetCompanyIdAsync();
            var (settings, bookingPage) = await _adminService.GetSettingsAsync(companyId);
            return Ok(ApiResponse<object>.Ok(new { Settings = settings, BookingPage = bookingPage }));
        }

        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] AppointmentSettingsDto dto)
        {
            int companyId = await GetCompanyIdAsync();
            var result = await _adminService.UpdateOperationalSettingsAsync(companyId, dto);
            return Ok(ApiResponse<AppointmentSettingsDto>.Ok(result));
        }

        [HttpPut("settings/operational")]
        public async Task<IActionResult> UpdateOperationalSettings([FromBody] AppointmentSettingsDto dto)
        {
            int companyId = await GetCompanyIdAsync();
            var result = await _adminService.UpdateOperationalSettingsAsync(companyId, dto);
            return Ok(ApiResponse<AppointmentSettingsDto>.Ok(result));
        }

        [HttpGet("booking-page")]
        public async Task<IActionResult> GetBookingPage()
        {
            int companyId = await GetCompanyIdAsync();
            var (_, bookingPage) = await _adminService.GetSettingsAsync(companyId);
            return Ok(ApiResponse<BookingPageSettingsDto>.Ok(bookingPage));
        }

        [HttpPut("booking-page")]
        public async Task<IActionResult> UpdateBookingPage([FromBody] BookingPageSettingsDto dto)
        {
            int companyId = await GetCompanyIdAsync();
            var result = await _adminService.UpdateBookingPageSettingsAsync(companyId, dto);
            return Ok(ApiResponse<BookingPageSettingsDto>.Ok(result));
        }

        [HttpPut("settings/booking-page")]
        public async Task<IActionResult> UpdateBookingPageSettings([FromBody] BookingPageSettingsDto dto)
        {
            int companyId = await GetCompanyIdAsync();
            var result = await _adminService.UpdateBookingPageSettingsAsync(companyId, dto);
            return Ok(ApiResponse<BookingPageSettingsDto>.Ok(result));
        }

        [HttpGet("availability")]
        public async Task<IActionResult> GetAvailability([FromQuery] int serviceId, [FromQuery] int? professionalId, [FromQuery] DateTime date)
        {
            int companyId = await GetCompanyIdAsync();
            var availability = await _availability.GetDayAvailabilityAsync(companyId, serviceId, professionalId, date);
            return Ok(ApiResponse<DayAvailabilityDto>.Ok(availability));
        }

        #endregion
    }

    public record InvoiceAppointmentRequest(int? CashSessionId = null, int? CustomerId = null, string? InvoiceFolio = null, int? PaymentMethodId = null);
    public record UpdateAppointmentStatusDto(string? Status, string? CancellationReason);
}
