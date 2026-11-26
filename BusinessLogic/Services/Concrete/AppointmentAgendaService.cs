using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Exceptions;
using Onion.Common.Features;
using Onion.Common.Models;
using Onion.Common.Services;
using Onion.DataAccess;
using Onion.Domain;
using Onion.Domain.Appointments;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class AppointmentAgendaService : IAppointmentAgendaService
    {
        private readonly OnionDbContext _db;
        private readonly IAvailabilityEngine _availability;
        private readonly IFeatureService _feature;
        private readonly IAgendaNotificationPublisher? _notificationPublisher;

        public AppointmentAgendaService(
            OnionDbContext db,
            IAvailabilityEngine availability,
            IFeatureService feature,
            IAgendaNotificationPublisher? notificationPublisher = null)
        {
            _db = db;
            _availability = availability;
            _feature = feature;
            _notificationPublisher = notificationPublisher;
        }

        #region Public Booking

        public async Task<ReservationResultDto> CreatePublicReservationAsync(string slug, CreatePublicReservationDto dto)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new CustomException(new Error { Code = "SLUG_REQUIRED", Message = "El identificador del negocio es requerido.", Language = "ES" });

            var cleanSlug = slug.Trim().ToLower();
            var bookingPage = await _db.BookingPageSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Slug.ToLower() == cleanSlug && !b.IsDeleted);

            if (bookingPage == null)
            {
                if (cleanSlug.StartsWith("empresa-") && int.TryParse(cleanSlug.Substring(8), out int cid))
                {
                    bookingPage = await _db.BookingPageSettings.AsNoTracking().FirstOrDefaultAsync(b => b.CompanyId == cid && !b.IsDeleted);
                }
                if (bookingPage == null)
                {
                    bookingPage = await _db.BookingPageSettings.AsNoTracking().FirstOrDefaultAsync(b => !b.IsDeleted && b.Active);
                }
            }

            if (bookingPage == null)
                throw new CustomException(new Error { Code = "BOOKING_PAGE_NOT_FOUND", Message = "La página de reservas no existe o está inactiva.", Language = "ES" });

            int companyId = bookingPage.CompanyId;
            var prevTenant = Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId;

            try
            {
                Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = companyId;

                // Service details
            var service = await _db.AppointmentServices
                .Include(s => s.Product)
                .Include(s => s.ProfessionalServices)
                .FirstOrDefaultAsync(s => (s.Id == dto.AppointmentServiceId || s.ProductId == dto.AppointmentServiceId) && s.CompanyId == companyId && !s.IsDeleted);

            if (service == null || service.Product == null)
            {
                var prod = await _db.Products.FirstOrDefaultAsync(p => p.Id == dto.AppointmentServiceId && p.CompanyId == companyId && !p.IsDeleted);
                if (prod != null)
                {
                    service = new Onion.Domain.Appointments.AppointmentService
                    {
                        CompanyId = companyId,
                        ProductId = prod.Id,
                        Product = prod,
                        DurationMinutes = 30,
                        BufferTimeMinutes = 0,
                        IsPubliclyBookable = true,
                        Active = true
                    };
                    await _db.AppointmentServices.AddAsync(service);
                    await _db.SaveChangesAsync();
                }
            }

            if (service == null || service.Product == null)
                throw new CustomException(new Error { Code = "SERVICE_NOT_FOUND", Message = "El servicio seleccionado no está disponible.", Language = "ES" });

            var settings = await _db.AppointmentSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CompanyId == companyId)
                ?? new AppointmentSettings { CompanyId = companyId };

            // Determine StartAt and EndAt
            var startAt = DateTime.SpecifyKind(dto.StartAt, DateTimeKind.Utc);
            int durationMinutes = service.DurationMinutes > 0 ? service.DurationMinutes : 30;
            int totalSlotDuration = durationMinutes + service.BufferTimeMinutes;
            var endAt = startAt.AddMinutes(totalSlotDuration);

            // Lead time validation
            if (startAt < DateTime.UtcNow.AddHours(settings.MinLeadTimeHours))
                throw new CustomException(new Error { Code = "MIN_LEAD_TIME_VIOLATION", Message = $"Debe reservar con al menos {settings.MinLeadTimeHours} hora(s) de anticipación.", Language = "ES" });

            // Resolve professional (specific or auto-assign)
            AppointmentProfessional? assignedProf = null;
            if (dto.ProfessionalId.HasValue && dto.ProfessionalId.Value > 0)
            {
                bool isFree = await _availability.IsProfessionalAvailableAsync(companyId, dto.ProfessionalId.Value, startAt, endAt);
                if (!isFree)
                    throw new CustomException(new Error { Code = "SLOT_NOT_AVAILABLE", Message = "El horario seleccionado ya no está disponible con el profesional elegido.", Language = "ES" });

                assignedProf = await _db.AppointmentProfessionals
                    .FirstOrDefaultAsync(p => p.Id == dto.ProfessionalId.Value && p.CompanyId == companyId && p.IsActive);
            }
            else
            {
                assignedProf = await _availability.AutoAssignProfessionalAsync(companyId, service.Id, startAt, endAt);
            }

            if (assignedProf == null)
            {
                assignedProf = await _db.AppointmentProfessionals
                    .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.IsActive && !p.IsDeleted);
            }

            if (assignedProf == null)
            {
                assignedProf = new AppointmentProfessional
                {
                    CompanyId = companyId,
                    DisplayName = "Personal General",
                    SpecialtyTitle = "Especialista General",
                    IsActive = true,
                    IsOwner = true,
                    DisplayOrder = 1
                };
                await _db.AppointmentProfessionals.AddAsync(assignedProf);
                await _db.SaveChangesAsync();

                await _db.AppointmentProfessionalServices.AddAsync(new AppointmentProfessionalService
                {
                    CompanyId = companyId,
                    AppointmentServiceId = service.Id,
                    ProfessionalId = assignedProf.Id,
                    IsActive = true
                });
                await _db.SaveChangesAsync();
            }

            // Generate cryptographically secure Management Token and human Booking Code
            string rawToken = Guid.NewGuid().ToString("N");
            string tokenHash = ComputeSha256(rawToken);
            DateTime tokenExpiry = startAt.AddDays(30);

            // Generate next Booking Code
            int countThisMonth = await _db.Appointments.CountAsync(a => a.CompanyId == companyId) + 1;
            string bookingCode = $"CIT-{countThisMonth:D6}";

            var status = settings.ConfirmationMode == AppointmentConfirmationMode.Automatic
                ? AppointmentStatus.Confirmed
                : AppointmentStatus.Pending;

            DateTime? expiresAt = status == AppointmentStatus.Pending
                ? DateTime.UtcNow.AddMinutes(settings.HoldSlotTimeoutMinutes > 0 ? settings.HoldSlotTimeoutMinutes : 30)
                : null;

            var appointment = new Appointment
            {
                CompanyId = companyId,
                BookingCode = bookingCode,
                ManagementTokenHash = tokenHash,
                ManagementTokenExpiresAt = tokenExpiry,
                StartAt = startAt,
                EndAt = endAt,
                Status = status,
                BillingStatus = AppointmentBillingStatus.Unbilled,
                Origin = AppointmentOrigin.PublicWeb,
                CustomerName = dto.CustomerName?.Trim() ?? string.Empty,
                CustomerPhone = dto.CustomerPhone?.Trim() ?? string.Empty,
                CustomerEmail = dto.CustomerEmail?.Trim(),
                AppointmentServiceId = service.Id,
                ProfessionalId = assignedProf.Id,
                ExpiresAt = expiresAt,
                Notes = dto.Notes?.Trim()
            };

            // Status history
            var history = new AppointmentStatusHistory
            {
                CompanyId = companyId,
                Appointment = appointment,
                PreviousStatus = status,
                NewStatus = status,
                ChangedByUserId = "PublicPortal",
                ChangeReason = status == AppointmentStatus.Confirmed ? "Reserva confirmada automáticamente" : "Solicitud de reserva recibida (pendiente de aprobación)"
            };

            await _db.Appointments.AddAsync(appointment);
            await _db.AppointmentStatusHistories.AddAsync(history);
            await _db.SaveChangesAsync();

            if (_notificationPublisher != null)
            {
                _ = _notificationPublisher.PublishAppointmentCreatedAsync(settings.CompanyId, new
                {
                    appointmentId = appointment.Id,
                    bookingCode = bookingCode,
                    customerName = appointment.CustomerName,
                    customerPhone = appointment.CustomerPhone,
                    serviceName = service.Product.Description,
                    startAt = appointment.StartAt,
                    status = status.ToString()
                });
            }

            return new ReservationResultDto(
                AppointmentId: appointment.Id,
                BookingCode: bookingCode,
                ManagementToken: rawToken,
                StartAt: startAt,
                EndAt: endAt,
                Status: status.ToString(),
                BillingStatus: appointment.BillingStatus.ToString(),
                ProfessionalId: assignedProf.Id,
                ProfessionalName: assignedProf.DisplayName,
                ServiceName: service.Product.Description,
                Price: service.Product.Price,
                DurationMinutes: service.DurationMinutes,
                Notes: appointment.Notes
            );
            }
            finally
            {
                Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = prevTenant;
            }
        }

        public async Task<AppointmentPublicStatusDto?> GetPublicAppointmentStatusAsync(string slug, string bookingCode, string managementToken)
        {
            var appointment = await FindAppointmentByTokenAsync(slug, bookingCode, managementToken);
            if (appointment == null) return null;

            return new AppointmentPublicStatusDto(
                AppointmentId: appointment.Id,
                BookingCode: appointment.BookingCode ?? string.Empty,
                StartAt: appointment.StartAt,
                EndAt: appointment.EndAt,
                Status: appointment.Status.ToString(),
                BillingStatus: appointment.BillingStatus.ToString(),
                CustomerName: appointment.CustomerName ?? string.Empty,
                CustomerPhone: appointment.CustomerPhone ?? string.Empty,
                ProfessionalName: appointment.Professional?.DisplayName ?? "Colaborador",
                ServiceName: appointment.AppointmentService?.Product?.Description ?? "Servicio",
                Price: appointment.AppointmentService?.Product?.Price ?? 0m,
                DurationMinutes: appointment.AppointmentService?.DurationMinutes ?? 30,
                CancellationReason: appointment.CancellationReason
            );
        }

        public async Task<bool> CancelPublicAppointmentAsync(string slug, string bookingCode, string managementToken, string reason)
        {
            var appointment = await FindAppointmentByTokenAsync(slug, bookingCode, managementToken);
            if (appointment == null)
                throw new CustomException(new Error { Code = "APPOINTMENT_NOT_FOUND", Message = "La cita no existe o el enlace de gestión es inválido.", Language = "ES" });

            if (appointment.Status == AppointmentStatus.Cancelled || appointment.Status == AppointmentStatus.Completed)
                throw new CustomException(new Error { Code = "INVALID_STATUS", Message = "La cita ya se encuentra finalizada o cancelada.", Language = "ES" });

            var settings = await _db.AppointmentSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CompanyId == appointment.CompanyId)
                ?? new AppointmentSettings { CompanyId = appointment.CompanyId };

            if (appointment.StartAt < DateTime.UtcNow.AddHours(settings.MinCancelLeadTimeHours))
                throw new CustomException(new Error { Code = "LATE_CANCELLATION", Message = $"Las cancelaciones deben realizarse con al menos {settings.MinCancelLeadTimeHours} horas de anticipación.", Language = "ES" });

            var prev = appointment.Status;
            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancellationReason = reason?.Trim();
            appointment.ExpiresAt = null;

            var history = new AppointmentStatusHistory
            {
                CompanyId = appointment.CompanyId,
                AppointmentId = appointment.Id,
                PreviousStatus = prev,
                NewStatus = AppointmentStatus.Cancelled,
                ChangedByUserId = "ClientToken",
                ChangeReason = reason?.Trim() ?? "Cancelada por el cliente"
            };

            await _db.AppointmentStatusHistories.AddAsync(history);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RequestPublicRescheduleAsync(string slug, string bookingCode, string managementToken, RescheduleRequestDto dto)
        {
            var appointment = await FindAppointmentByTokenAsync(slug, bookingCode, managementToken);
            if (appointment == null)
                throw new CustomException(new Error { Code = "APPOINTMENT_NOT_FOUND", Message = "La cita no existe o el enlace de gestión es inválido.", Language = "ES" });

            if (appointment.Status != AppointmentStatus.Confirmed && appointment.Status != AppointmentStatus.Pending)
                throw new CustomException(new Error { Code = "INVALID_STATUS", Message = "Solo se pueden reprogramar citas activas o pendientes.", Language = "ES" });

            var newStart = DateTime.SpecifyKind(dto.NewStartAt, DateTimeKind.Utc);
            int duration = appointment.AppointmentService?.DurationMinutes ?? 30;
            int buffer = appointment.AppointmentService?.BufferTimeMinutes ?? 0;
            var newEnd = newStart.AddMinutes(duration + buffer);

            if (appointment.ProfessionalId.HasValue)
            {
                bool isFree = await _availability.IsProfessionalAvailableAsync(appointment.CompanyId, appointment.ProfessionalId.Value, newStart, newEnd, appointment.Id);
                if (!isFree)
                    throw new CustomException(new Error { Code = "SLOT_NOT_AVAILABLE", Message = "El nuevo horario solicitado no está disponible.", Language = "ES" });
            }

            var request = new AppointmentRescheduleRequest
            {
                CompanyId = appointment.CompanyId,
                AppointmentId = appointment.Id,
                ProposedStartAt = newStart,
                ProposedEndAt = newEnd,
                RequestedBy = "Client",
                Status = "Pending",
                Notes = dto.Reason?.Trim()
            };

            var prev = appointment.Status;
            appointment.Status = AppointmentStatus.RescheduleProposed;

            var history = new AppointmentStatusHistory
            {
                CompanyId = appointment.CompanyId,
                AppointmentId = appointment.Id,
                PreviousStatus = prev,
                NewStatus = AppointmentStatus.RescheduleProposed,
                ChangedByUserId = "ClientToken",
                ChangeReason = $"Solicitud de cambio a {newStart:dd/MM/yyyy HH:mm}. Motivo: {dto.Reason}"
            };

            await _db.AppointmentRescheduleRequests.AddAsync(request);
            await _db.AppointmentStatusHistories.AddAsync(history);
            await _db.SaveChangesAsync();
            return true;
        }

        #endregion

        #region State Machine Transitions (Administrative)

        public async Task<AppointmentFullDetailDto> ConfirmAppointmentAsync(int companyId, int appointmentId, string? userId = null)
        {
            var appointment = await GetAppointmentEntityAsync(companyId, appointmentId);
            ValidateStateTransition(appointment.Status, AppointmentStatus.Confirmed);

            var prev = appointment.Status;
            appointment.Status = AppointmentStatus.Confirmed;
            appointment.ExpiresAt = null; // Remove temporary reservation timeout

            var history = new AppointmentStatusHistory
            {
                CompanyId = companyId,
                AppointmentId = appointmentId,
                PreviousStatus = prev,
                NewStatus = AppointmentStatus.Confirmed,
                ChangedByUserId = userId ?? "Admin",
                ChangeReason = "Aprobada manualmente por el negocio"
            };

            await _db.AppointmentStatusHistories.AddAsync(history);
            await _db.SaveChangesAsync();

            return MapToDetailDto(appointment);
        }

        public async Task<AppointmentFullDetailDto> RejectAppointmentAsync(int companyId, int appointmentId, string reason, string? userId = null)
        {
            var appointment = await GetAppointmentEntityAsync(companyId, appointmentId);
            ValidateStateTransition(appointment.Status, AppointmentStatus.Rejected);

            var prev = appointment.Status;
            appointment.Status = AppointmentStatus.Rejected;
            appointment.CancellationReason = reason?.Trim();
            appointment.ExpiresAt = null;

            var history = new AppointmentStatusHistory
            {
                CompanyId = companyId,
                AppointmentId = appointmentId,
                PreviousStatus = prev,
                NewStatus = AppointmentStatus.Rejected,
                ChangedByUserId = userId ?? "Admin",
                ChangeReason = reason?.Trim() ?? "Rechazada por el negocio"
            };

            await _db.AppointmentStatusHistories.AddAsync(history);
            await _db.SaveChangesAsync();

            return MapToDetailDto(appointment);
        }

        public async Task<AppointmentFullDetailDto> StartAppointmentAsync(int companyId, int appointmentId, string? userId = null)
        {
            var appointment = await GetAppointmentEntityAsync(companyId, appointmentId);
            ValidateStateTransition(appointment.Status, AppointmentStatus.InProgress);

            var prev = appointment.Status;
            appointment.Status = AppointmentStatus.InProgress;

            var history = new AppointmentStatusHistory
            {
                CompanyId = companyId,
                AppointmentId = appointmentId,
                PreviousStatus = prev,
                NewStatus = AppointmentStatus.InProgress,
                ChangedByUserId = userId ?? "Admin",
                ChangeReason = "Cliente llegó y comenzó la atención"
            };

            await _db.AppointmentStatusHistories.AddAsync(history);
            await _db.SaveChangesAsync();

            return MapToDetailDto(appointment);
        }

        public async Task<AppointmentFullDetailDto> CompleteAppointmentAsync(int companyId, int appointmentId, string? userId = null)
        {
            var appointment = await GetAppointmentEntityAsync(companyId, appointmentId);
            ValidateStateTransition(appointment.Status, AppointmentStatus.Completed);

            var prev = appointment.Status;
            appointment.Status = AppointmentStatus.Completed;
            // Kept as Unbilled until POS sale checkout
            appointment.BillingStatus = appointment.SaleId.HasValue ? AppointmentBillingStatus.Billed : AppointmentBillingStatus.Unbilled;

            var history = new AppointmentStatusHistory
            {
                CompanyId = companyId,
                AppointmentId = appointmentId,
                PreviousStatus = prev,
                NewStatus = AppointmentStatus.Completed,
                ChangedByUserId = userId ?? "Admin",
                ChangeReason = "Atención concluida"
            };

            await _db.AppointmentStatusHistories.AddAsync(history);
            await _db.SaveChangesAsync();

            return MapToDetailDto(appointment);
        }

        public async Task<AppointmentFullDetailDto> MarkNoShowAsync(int companyId, int appointmentId, string? userId = null)
        {
            var appointment = await GetAppointmentEntityAsync(companyId, appointmentId);
            ValidateStateTransition(appointment.Status, AppointmentStatus.NoShow);

            var prev = appointment.Status;
            appointment.Status = AppointmentStatus.NoShow;

            var history = new AppointmentStatusHistory
            {
                CompanyId = companyId,
                AppointmentId = appointmentId,
                PreviousStatus = prev,
                NewStatus = AppointmentStatus.NoShow,
                ChangedByUserId = userId ?? "Admin",
                ChangeReason = "Cliente no asistió a la cita"
            };

            await _db.AppointmentStatusHistories.AddAsync(history);
            await _db.SaveChangesAsync();

            return MapToDetailDto(appointment);
        }

        public async Task<AppointmentFullDetailDto> CancelAdminAppointmentAsync(int companyId, int appointmentId, string reason, string? userId = null)
        {
            var appointment = await GetAppointmentEntityAsync(companyId, appointmentId);
            ValidateStateTransition(appointment.Status, AppointmentStatus.Cancelled);

            var prev = appointment.Status;
            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancellationReason = reason?.Trim();
            appointment.ExpiresAt = null;

            var history = new AppointmentStatusHistory
            {
                CompanyId = companyId,
                AppointmentId = appointmentId,
                PreviousStatus = prev,
                NewStatus = AppointmentStatus.Cancelled,
                ChangedByUserId = userId ?? "Admin",
                ChangeReason = reason?.Trim() ?? "Cancelada administrativamente"
            };

            await _db.AppointmentStatusHistories.AddAsync(history);
            await _db.SaveChangesAsync();

            return MapToDetailDto(appointment);
        }

        public async Task<AppointmentFullDetailDto> RescheduleAppointmentAdminAsync(int companyId, int appointmentId, DateTime newStartAt, string? userId = null)
        {
            var appointment = await GetAppointmentEntityAsync(companyId, appointmentId);
            if (appointment.Status == AppointmentStatus.Cancelled || appointment.Status == AppointmentStatus.Completed)
                throw new CustomException(new Error { Code = "INVALID_STATUS", Message = "No se puede reprogramar una cita cancelada o completada.", Language = "ES" });

            var newStart = DateTime.SpecifyKind(newStartAt, DateTimeKind.Utc);
            int duration = appointment.AppointmentService?.DurationMinutes ?? 30;
            int buffer = appointment.AppointmentService?.BufferTimeMinutes ?? 0;
            var newEnd = newStart.AddMinutes(duration + buffer);

            if (appointment.ProfessionalId.HasValue)
            {
                bool isFree = await _availability.IsProfessionalAvailableAsync(companyId, appointment.ProfessionalId.Value, newStart, newEnd, appointment.Id);
                if (!isFree)
                    throw new CustomException(new Error { Code = "SLOT_NOT_AVAILABLE", Message = "El profesional ya tiene un compromiso en el nuevo horario.", Language = "ES" });
            }

            var prevStart = appointment.StartAt;
            var prevStatus = appointment.Status;
            appointment.StartAt = newStart;
            appointment.EndAt = newEnd;
            appointment.Status = AppointmentStatus.Confirmed;
            appointment.ExpiresAt = null;

            var history = new AppointmentStatusHistory
            {
                CompanyId = companyId,
                AppointmentId = appointmentId,
                PreviousStatus = prevStatus,
                NewStatus = AppointmentStatus.Confirmed,
                ChangedByUserId = userId ?? "Admin",
                ChangeReason = $"Reprogramada de {prevStart:dd/MM/yyyy HH:mm} a {newStart:dd/MM/yyyy HH:mm}"
            };

            await _db.AppointmentStatusHistories.AddAsync(history);
            await _db.SaveChangesAsync();

            return MapToDetailDto(appointment);
        }

        #endregion

        #region Invoicing Integration (POS 1-to-1)

        public async Task<int> InvoiceAppointmentAsync(int companyId, int appointmentId, int cashSessionId, int? customerId, string? invoiceFolio)
        {
            var appointment = await GetAppointmentEntityAsync(companyId, appointmentId);

            if (appointment.BillingStatus == AppointmentBillingStatus.Billed || appointment.SaleId.HasValue)
                throw new CustomException(new Error { Code = "ALREADY_BILLED", Message = "Esta cita ya fue facturada anteriormente.", Language = "ES" });

            if (appointment.Status != AppointmentStatus.Completed && appointment.Status != AppointmentStatus.Confirmed)
                throw new CustomException(new Error { Code = "CANNOT_BILL_STATUS", Message = "Solo se pueden facturar citas confirmadas o completadas.", Language = "ES" });

            if (appointment.AppointmentService == null || appointment.AppointmentService.Product == null)
                throw new CustomException(new Error { Code = "SERVICE_MISSING", Message = "No se encontró el servicio asociado a la cita.", Language = "ES" });

            var product = appointment.AppointmentService.Product;
            decimal price = product.Price;
            decimal taxRate = 0.18m;
            decimal tax = Math.Round(price * taxRate, 2);
            decimal total = price + tax;

            var sale = new Sale
            {
                CompanyId = companyId,
                CashSessionId = cashSessionId > 0 ? cashSessionId : null,
                CustomerId = customerId ?? appointment.CustomerId ?? 1,
                Date = DateTime.UtcNow,
                SubTotal = price,
                Tax = tax,
                Total = total,
                PaidAmount = total,
                Status = SaleStatus.PAID,
                PaymentType = PaymentType.CASH,
                PaymentMethod = PaymentMethod.CASH,
                OriginChannel = SaleOriginChannel.ADMINISTRATIVO,
                InvoiceFolio = invoiceFolio ?? $"FAC-CIT-{appointment.BookingCode}",
                Notes = $"Facturación de Cita {appointment.BookingCode} ({appointment.CustomerName})",
                Details = new List<SaleDetail>
                {
                    new SaleDetail
                    {
                        ProductId = product.Id,
                        Quantity = 1,
                        OriginalQuantity = 1,
                        UnitPrice = price,
                        Discount = 0,
                        Tax = tax
                    }
                }
            };

            await _db.Sales.AddAsync(sale);
            await _db.SaveChangesAsync(); // Generates sale.Id

            // Link appointment univoquely to sale
            appointment.SaleId = sale.Id;
            appointment.BillingStatus = AppointmentBillingStatus.Billed;
            if (appointment.Status != AppointmentStatus.Completed)
            {
                appointment.Status = AppointmentStatus.Completed;
            }

            var history = new AppointmentStatusHistory
            {
                CompanyId = companyId,
                AppointmentId = appointmentId,
                PreviousStatus = appointment.Status,
                NewStatus = AppointmentStatus.Completed,
                ChangedByUserId = "POS",
                ChangeReason = $"Facturada exitosamente bajo comprobante {sale.InvoiceFolio} (Venta #{sale.Id})"
            };

            await _db.AppointmentStatusHistories.AddAsync(history);
            await _db.SaveChangesAsync();

            return sale.Id;
        }

        #endregion

        #region Queries

        public async Task<AppointmentFullDetailDto?> GetAppointmentByIdAsync(int companyId, int appointmentId)
        {
            var a = await _db.Appointments
                .Include(x => x.AppointmentService)
                    .ThenInclude(s => s!.Product)
                .Include(x => x.Professional)
                .Include(x => x.Customer)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == appointmentId && x.CompanyId == companyId);

            return a == null ? null : MapToDetailDto(a);
        }

        public async Task<IEnumerable<AppointmentFullDetailDto>> ListAppointmentsAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null, string? status = null, int? professionalId = null)
        {
            var query = _db.Appointments
                .Include(x => x.AppointmentService)
                    .ThenInclude(s => s!.Product)
                .Include(x => x.Professional)
                .Include(x => x.Customer)
                .Where(x => x.CompanyId == companyId);

            if (fromDate.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(x => x.StartAt >= fromUtc);
            }

            if (toDate.HasValue)
            {
                var toUtc = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(x => x.StartAt <= toUtc);
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AppointmentStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(x => x.Status == parsedStatus);
            }

            if (professionalId.HasValue && professionalId.Value > 0)
            {
                query = query.Where(x => x.ProfessionalId == professionalId.Value);
            }

            var list = await query
                .OrderByDescending(x => x.StartAt)
                .AsNoTracking()
                .ToListAsync();

            return list.Select(MapToDetailDto);
        }

        #endregion

        #region Helpers

        private async Task<Appointment?> FindAppointmentByTokenAsync(string slug, string bookingCode, string managementToken)
        {
            if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(bookingCode) || string.IsNullOrWhiteSpace(managementToken))
                return null;

            var bookingPage = await _db.BookingPageSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Slug.ToLower() == slug.ToLower() && b.IsPublicActive);

            if (bookingPage == null) return null;

            string tokenHash = ComputeSha256(managementToken);

            return await _db.Appointments
                .Include(a => a.AppointmentService)
                    .ThenInclude(s => s!.Product)
                .Include(a => a.Professional)
                .FirstOrDefaultAsync(a => a.CompanyId == bookingPage.CompanyId
                                       && a.BookingCode == bookingCode
                                       && a.ManagementTokenHash == tokenHash
                                       && a.ManagementTokenExpiresAt >= DateTime.UtcNow);
        }

        private async Task<Appointment> GetAppointmentEntityAsync(int companyId, int appointmentId)
        {
            var appointment = await _db.Appointments
                .Include(a => a.AppointmentService)
                    .ThenInclude(s => s!.Product)
                .Include(a => a.Professional)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.CompanyId == companyId);

            if (appointment == null)
                throw new CustomException(new Error { Code = "APPOINTMENT_NOT_FOUND", Message = "La cita especificada no fue encontrada.", Language = "ES" });

            return appointment;
        }

        private static void ValidateStateTransition(AppointmentStatus current, AppointmentStatus target)
        {
            bool valid = (current, target) switch
            {
                // Pending transitions
                (AppointmentStatus.Pending, AppointmentStatus.Confirmed) => true,
                (AppointmentStatus.Pending, AppointmentStatus.Rejected) => true,
                (AppointmentStatus.Pending, AppointmentStatus.Cancelled) => true,
                (AppointmentStatus.Pending, AppointmentStatus.Expired) => true,

                // Confirmed transitions
                (AppointmentStatus.Confirmed, AppointmentStatus.InProgress) => true,
                (AppointmentStatus.Confirmed, AppointmentStatus.Completed) => true,
                (AppointmentStatus.Confirmed, AppointmentStatus.RescheduleProposed) => true,
                (AppointmentStatus.Confirmed, AppointmentStatus.Cancelled) => true,
                (AppointmentStatus.Confirmed, AppointmentStatus.NoShow) => true,

                // Reschedule proposed transitions
                (AppointmentStatus.RescheduleProposed, AppointmentStatus.Confirmed) => true,
                (AppointmentStatus.RescheduleProposed, AppointmentStatus.Cancelled) => true,

                // InProgress transitions
                (AppointmentStatus.InProgress, AppointmentStatus.Completed) => true,
                (AppointmentStatus.InProgress, AppointmentStatus.Cancelled) => true,

                _ => false
            };

            if (!valid)
            {
                throw new CustomException(new Error
                {
                    Code = "INVALID_STATE_TRANSITION",
                    Message = $"No se permite la transición de estado desde '{current}' hacia '{target}'.",
                    Language = "ES"
                });
            }
        }

        private static AppointmentFullDetailDto MapToDetailDto(Appointment a)
        {
            return new AppointmentFullDetailDto(
                Id: a.Id,
                BookingCode: a.BookingCode,
                StartAt: a.StartAt,
                EndAt: a.EndAt,
                Status: a.Status.ToString(),
                BillingStatus: a.BillingStatus.ToString(),
                Origin: a.Origin.ToString(),
                CustomerId: a.CustomerId,
                CustomerName: a.CustomerName ?? a.Customer?.Name ?? string.Empty,
                CustomerPhone: a.CustomerPhone ?? a.Customer?.Phone ?? string.Empty,
                CustomerEmail: a.CustomerEmail ?? a.Customer?.Email,
                AppointmentServiceId: a.AppointmentServiceId,
                ServiceName: a.AppointmentService?.Product?.Description ?? "Servicio",
                Price: a.AppointmentService?.Product?.Price ?? 0m,
                DurationMinutes: a.AppointmentService?.DurationMinutes ?? 30,
                ProfessionalId: a.ProfessionalId,
                ProfessionalName: a.Professional?.DisplayName ?? "Colaborador",
                SaleId: a.SaleId,
                ExpiresAt: a.ExpiresAt,
                Notes: a.Notes,
                CancellationReason: a.CancellationReason,
                CreationDate: a.CreationDate
            );
        }

        public static string ComputeSha256(string rawData)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }
            return sb.ToString();
        }

        #endregion
    }
}
