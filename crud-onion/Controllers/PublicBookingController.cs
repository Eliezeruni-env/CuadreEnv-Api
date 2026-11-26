using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Exceptions;
using Onion.Common.Models;
using Onion.DataAccess;

namespace Onion.Controllers
{
    [Route("v1/public/booking/{slug}")]
    [Route("public/booking/{slug}")]
    [Route("api/public/booking/{slug}")]
    [ApiController]
    [AllowAnonymous]
    public class PublicBookingController : ControllerBase
    {
        private readonly IAgendaAdminService _adminService;
        private readonly IAppointmentAgendaService _agendaService;
        private readonly IAvailabilityEngine _availability;
        private readonly OnionDbContext _db;

        public PublicBookingController(
            IAgendaAdminService adminService,
            IAppointmentAgendaService agendaService,
            IAvailabilityEngine availability,
            OnionDbContext db)
        {
            _adminService = adminService;
            _agendaService = agendaService;
            _availability = availability;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return BadRequest(ApiResponse<object>.Fail("Slug requerido."));

            var profile = await _adminService.GetPublicBusinessProfileAsync(slug);
            if (profile == null)
                return NotFound(ApiResponse<object>.Fail("Página de reservas no encontrada o temporalmente inactiva."));

            return Ok(ApiResponse<PublicBusinessProfileDto>.Ok(profile));
        }

        [HttpGet("slots")]
        public async Task<IActionResult> GetSlots(string slug, [FromQuery] int serviceId, [FromQuery] int? professionalId, [FromQuery] DateTime date)
        {
            if (string.IsNullOrWhiteSpace(slug) || serviceId <= 0)
                return BadRequest(ApiResponse<object>.Fail("Parámetros requeridos inválidos."));

            var bookingPage = await _adminService.ResolveBookingPageAsync(slug);

            if (bookingPage == null)
                return NotFound(ApiResponse<object>.Fail("Página de reservas no encontrada o inactiva."));

            var availability = await _availability.GetDayAvailabilityAsync(bookingPage.CompanyId, serviceId, professionalId, date);
            return Ok(ApiResponse<DayAvailabilityDto>.Ok(availability));
        }

        [HttpPost("reserve")]
        public async Task<IActionResult> Reserve(string slug, [FromBody] CreatePublicReservationDto dto)
        {
            if (dto == null)
                return BadRequest(ApiResponse<object>.Fail("Datos de reserva requeridos."));

            if (string.IsNullOrWhiteSpace(dto.CustomerName) || string.IsNullOrWhiteSpace(dto.CustomerPhone))
                return BadRequest(ApiResponse<object>.Fail("Nombre y teléfono de contacto son obligatorios."));

            var result = await _agendaService.CreatePublicReservationAsync(slug, dto);
            return Ok(ApiResponse<ReservationResultDto>.Ok(result));
        }

        [HttpGet("status/{bookingCode}")]
        public async Task<IActionResult> GetStatus(string slug, string bookingCode, [FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(bookingCode) || string.IsNullOrWhiteSpace(token))
                return BadRequest(ApiResponse<object>.Fail("Código de reserva y token de seguridad son requeridos."));

            var status = await _agendaService.GetPublicAppointmentStatusAsync(slug, bookingCode, token);
            if (status == null)
                return NotFound(ApiResponse<object>.Fail("Cita no encontrada o el enlace de gestión ha expirado."));

            return Ok(ApiResponse<AppointmentPublicStatusDto>.Ok(status));
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> Cancel(string slug, [FromBody] PublicCancelRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.BookingCode) || string.IsNullOrWhiteSpace(dto.ManagementToken))
                return BadRequest(ApiResponse<object>.Fail("Código de reserva y token son requeridos."));

            bool cancelled = await _agendaService.CancelPublicAppointmentAsync(slug, dto.BookingCode, dto.ManagementToken, dto.Reason);
            return Ok(ApiResponse<object>.Ok(new { Success = cancelled, Message = "Cita cancelada exitosamente." }));
        }

        [HttpPost("reschedule")]
        public async Task<IActionResult> Reschedule(string slug, [FromBody] PublicRescheduleRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.BookingCode) || string.IsNullOrWhiteSpace(dto.ManagementToken))
                return BadRequest(ApiResponse<object>.Fail("Código de reserva y token son requeridos."));

            var request = new RescheduleRequestDto(dto.NewStartAt, dto.Reason);
            bool requested = await _agendaService.RequestPublicRescheduleAsync(slug, dto.BookingCode, dto.ManagementToken, request);
            return Ok(ApiResponse<object>.Ok(new { Success = requested, Message = "Solicitud de reprogramación enviada exitosamente." }));
        }
    }
}
