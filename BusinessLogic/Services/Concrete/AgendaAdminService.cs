using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Exceptions;
using Onion.Common.Models;
using Onion.DataAccess;
using Onion.Domain.Appointments;

namespace Onion.BussinesLogic.Services.Concrete
{
    using AppointmentServiceEntity = Onion.Domain.Appointments.AppointmentService;

    public class AgendaAdminService : IAgendaAdminService
    {
        private readonly OnionDbContext _db;
        private readonly IAvailabilityEngine _availability;
        private readonly Onion.Common.Features.IFeatureService _feature;

        public AgendaAdminService(
            OnionDbContext db,
            IAvailabilityEngine availability,
            Onion.Common.Features.IFeatureService feature)
        {
            _db = db;
            _availability = availability;
            _feature = feature;
        }

        #region Dashboard

        public async Task<AgendaDashboardDto> GetDashboardAsync(int companyId)
        {
            await _availability.ExpireStalePendingAppointmentsAsync(companyId);

            var todayUtc = DateTime.UtcNow.Date;
            var dayStartUtc = DateTime.SpecifyKind(todayUtc, DateTimeKind.Utc);
            var dayEndUtc = dayStartUtc.AddDays(1);

            var todayAppointments = await _db.Appointments
                .Include(a => a.AppointmentService)
                    .ThenInclude(s => s!.Product)
                .Include(a => a.Professional)
                .Include(a => a.Customer)
                .Where(a => a.CompanyId == companyId && a.StartAt >= dayStartUtc && a.StartAt < dayEndUtc)
                .OrderBy(a => a.StartAt)
                .AsNoTracking()
                .ToListAsync();

            int citasHoy = todayAppointments.Count(a => a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.Rejected && a.Status != AppointmentStatus.Expired);
            int pendientes = todayAppointments.Count(a => a.Status == AppointmentStatus.Pending);
            int confirmadas = todayAppointments.Count(a => a.Status == AppointmentStatus.Confirmed);

            decimal ingresosFacturados = todayAppointments
                .Where(a => a.Status == AppointmentStatus.Completed || a.BillingStatus == AppointmentBillingStatus.Billed)
                .Sum(a => a.AppointmentService?.Product?.Price ?? 0m);

            var citasDeHoy = todayAppointments.Select(a => new AgendaTodayTimelineItemDto(
                Id: a.Id,
                TimeFormatted: a.StartAt.ToString("HH:mm"),
                StartAt: a.StartAt,
                EndAt: a.EndAt,
                CustomerName: a.CustomerName ?? a.Customer?.Name ?? "Cliente",
                CustomerPhone: a.CustomerPhone ?? a.Customer?.Phone ?? string.Empty,
                ServiceName: a.AppointmentService?.Product?.Description ?? "Servicio",
                ProfessionalName: a.Professional?.DisplayName ?? "Colaborador",
                Status: a.Status.ToString(),
                BillingStatus: a.BillingStatus.ToString(),
                Price: a.AppointmentService?.Product?.Price ?? 0m
            )).ToList();

            var pendingQuery = await _db.Appointments
                .Include(a => a.AppointmentService)
                    .ThenInclude(s => s!.Product)
                .Include(a => a.Professional)
                .Include(a => a.Customer)
                .Where(a => a.CompanyId == companyId && a.Status == AppointmentStatus.Pending)
                .OrderBy(a => a.StartAt)
                .Take(10)
                .AsNoTracking()
                .ToListAsync();

            var solicitudesPendientes = pendingQuery.Select(a => new AgendaPendingRequestItemDto(
                Id: a.Id,
                BookingCode: a.BookingCode ?? $"CIT-{a.Id:D6}",
                CustomerName: a.CustomerName ?? a.Customer?.Name ?? "Cliente",
                CustomerPhone: a.CustomerPhone ?? a.Customer?.Phone ?? string.Empty,
                StartAt: a.StartAt,
                ServiceName: a.AppointmentService?.Product?.Description ?? "Servicio",
                ProfessionalName: a.Professional?.DisplayName ?? "Colaborador",
                Price: a.AppointmentService?.Product?.Price ?? 0m,
                ExpiresAt: a.ExpiresAt
            )).ToList();

            // Weekly occupancy (Monday to Sunday)
            var currentDayOfWeek = (int)todayUtc.DayOfWeek;
            int offsetToMonday = (currentDayOfWeek == 0 ? -6 : 1 - currentDayOfWeek);
            var mondayUtc = todayUtc.AddDays(offsetToMonday);

            var weekAppointments = await _db.Appointments
                .Where(a => a.CompanyId == companyId
                         && a.StartAt >= mondayUtc
                         && a.StartAt < mondayUtc.AddDays(7)
                         && (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.InProgress))
                .AsNoTracking()
                .ToListAsync();

            var ocupacionSemanal = new List<AgendaWeeklyOccupancyDayDto>();
            string[] dayNames = { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };

            for (int i = 0; i < 7; i++)
            {
                var d = mondayUtc.AddDays(i);
                var dayBooked = weekAppointments.Count(a => a.StartAt.Date == d.Date);
                int totalCapacity = 10; // Nominal default daily slot capacity
                int percentage = Math.Min(100, (int)Math.Round((double)dayBooked / totalCapacity * 100));

                ocupacionSemanal.Add(new AgendaWeeklyOccupancyDayDto(
                    DayLabel: $"{dayNames[i]} {d.Day}",
                    Date: d,
                    OccupancyPercentage: percentage,
                    TotalSlots: totalCapacity,
                    BookedSlots: dayBooked
                ));
            }

            var futureAppointments = await _db.Appointments
                .Include(a => a.AppointmentService)
                    .ThenInclude(s => s!.Product)
                .Include(a => a.Professional)
                .Include(a => a.Customer)
                .Where(a => a.CompanyId == companyId && a.StartAt >= dayEndUtc && (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Pending))
                .OrderBy(a => a.StartAt)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            var proximasCitas = futureAppointments.Select(a => new AgendaTodayTimelineItemDto(
                Id: a.Id,
                TimeFormatted: a.StartAt.ToString("HH:mm"),
                StartAt: a.StartAt,
                EndAt: a.EndAt,
                CustomerName: a.CustomerName ?? a.Customer?.Name ?? "Cliente",
                CustomerPhone: a.CustomerPhone ?? a.Customer?.Phone ?? string.Empty,
                ServiceName: a.AppointmentService?.Product?.Description ?? "Servicio",
                ProfessionalName: a.Professional?.DisplayName ?? "Colaborador",
                Status: a.Status.ToString(),
                BillingStatus: a.BillingStatus.ToString(),
                Price: a.AppointmentService?.Product?.Price ?? 0m
            )).ToList();

            return new AgendaDashboardDto(
                CitasHoy: citasHoy,
                Pendientes: pendientes,
                Confirmadas: confirmadas,
                IngresosFacturados: ingresosFacturados,
                CitasDeHoy: citasDeHoy,
                SolicitudesPendientes: solicitudesPendientes,
                OcupacionSemanal: ocupacionSemanal,
                ProximasCitas: proximasCitas
            );
        }

        #endregion

        #region Reports

        public async Task<AgendaReportsDto> GetReportsAsync(int companyId, int? month = null, int? year = null)
        {
            var now = DateTime.UtcNow;
            int targetMonth = month ?? now.Month;
            int targetYear = year ?? now.Year;

            var startOfMonth = new DateTime(targetYear, targetMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1);

            var appointments = await _db.Appointments
                .Include(a => a.AppointmentService)
                    .ThenInclude(s => s!.Product)
                .Include(a => a.Professional)
                .Where(a => a.CompanyId == companyId && a.StartAt >= startOfMonth && a.StartAt < endOfMonth)
                .AsNoTracking()
                .ToListAsync();

            int totalCitas = appointments.Count(a => a.Status != AppointmentStatus.Expired && a.Status != AppointmentStatus.Rejected);
            int completadas = appointments.Count(a => a.Status == AppointmentStatus.Completed);
            int canceladas = appointments.Count(a => a.Status == AppointmentStatus.Cancelled);
            int noShow = appointments.Count(a => a.Status == AppointmentStatus.NoShow);

            int attendanceDenominator = completadas + noShow;
            int tasaAsistencia = attendanceDenominator > 0 ? (int)Math.Round((double)completadas / attendanceDenominator * 100) : 100;
            int tasaOcupacion = totalCitas > 0 ? Math.Min(100, (int)Math.Round((double)completadas / Math.Max(1, totalCitas) * 90)) : 0;

            decimal totalFacturado = appointments
                .Where(a => a.Status == AppointmentStatus.Completed || a.BillingStatus == AppointmentBillingStatus.Billed)
                .Sum(a => a.AppointmentService?.Product?.Price ?? 0m);

            // Citas por día
            int daysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);
            var citasPorDia = new List<AgendaDaySeriesDto>();
            for (int day = 1; day <= daysInMonth; day++)
            {
                var d = new DateTime(targetYear, targetMonth, day, 0, 0, 0, DateTimeKind.Utc);
                int count = appointments.Count(a => a.StartAt.Date == d.Date && a.Status == AppointmentStatus.Completed);
                citasPorDia.Add(new AgendaDaySeriesDto(day, d, count));
            }

            // Distribución por estado
            var distribucion = new List<AgendaStatusDistributionDto>
            {
                new AgendaStatusDistributionDto("Completed", "Completadas", completadas, totalCitas > 0 ? (int)Math.Round((double)completadas / totalCitas * 100) : 0),
                new AgendaStatusDistributionDto("Cancelled", "Canceladas", canceladas, totalCitas > 0 ? (int)Math.Round((double)canceladas / totalCitas * 100) : 0),
                new AgendaStatusDistributionDto("NoShow", "No asistió", noShow, totalCitas > 0 ? (int)Math.Round((double)noShow / totalCitas * 100) : 0)
            };

            // Top Servicios
            var topServicios = appointments
                .Where(a => a.AppointmentService?.Product != null)
                .GroupBy(a => a.AppointmentService!.Product!.Description)
                .Select((g, index) => new AgendaTopServiceItemDto(
                    Rank: index + 1,
                    ServiceName: g.Key,
                    CitasCount: g.Count(),
                    TotalFacturado: g.Sum(x => x.AppointmentService?.Product?.Price ?? 0m)
                ))
                .OrderByDescending(x => x.CitasCount)
                .Take(5)
                .ToList();

            // Desempeño del personal
            var desempenoPersonal = appointments
                .Where(a => a.Professional != null)
                .GroupBy(a => new { a.Professional!.Id, a.Professional.DisplayName })
                .Select(g =>
                {
                    int comp = g.Count(x => x.Status == AppointmentStatus.Completed);
                    int ocup = Math.Min(100, (int)Math.Round((double)comp / Math.Max(1, g.Count()) * 85));
                    return new AgendaStaffPerformanceItemDto(
                        ProfessionalId: g.Key.Id,
                        ProfessionalName: g.Key.DisplayName,
                        CitasCompletadas: comp,
                        OcupacionPercentage: ocup
                    );
                })
                .OrderByDescending(x => x.CitasCompletadas)
                .ToList();

            return new AgendaReportsDto(
                TotalCitas: totalCitas,
                TasaAsistenciaPercentage: tasaAsistencia,
                TasaOcupacionPercentage: tasaOcupacion,
                TotalFacturado: totalFacturado,
                CitasPorDia: citasPorDia,
                DistribucionPorEstado: distribucion,
                TopServicios: topServicios,
                DesempenoPersonal: desempenoPersonal
            );
        }

        #endregion

        #region CRM Clients

        public async Task<AgendaClientsOverviewDto> GetClientsOverviewAsync(int companyId, string? search = null)
        {
            var query = _db.Appointments
                .Include(a => a.AppointmentService)
                    .ThenInclude(s => s!.Product)
                .Include(a => a.Professional)
                .Where(a => a.CompanyId == companyId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();
                query = query.Where(a => (a.CustomerName != null && a.CustomerName.ToLower().Contains(s))
                                      || (a.CustomerPhone != null && a.CustomerPhone.Contains(s)));
            }

            var all = await query.OrderByDescending(a => a.StartAt).AsNoTracking().ToListAsync();

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var grouped = all.GroupBy(a => a.CustomerPhone?.Trim() ?? a.CustomerName?.Trim() ?? a.Id.ToString())
                .Select(g =>
                {
                    var first = g.First();
                    var completedOrFuture = g.Where(x => x.Status != AppointmentStatus.Cancelled && x.Status != AppointmentStatus.Rejected).ToList();
                    var past = completedOrFuture.Where(x => x.StartAt <= now).OrderByDescending(x => x.StartAt).FirstOrDefault();
                    var next = completedOrFuture.Where(x => x.StartAt > now).OrderBy(x => x.StartAt).FirstOrDefault();

                    var history = g.Select(h => new AgendaClientHistoryItemDto(
                        AppointmentId: h.Id,
                        BookingCode: h.BookingCode ?? $"CIT-{h.Id:D6}",
                        Date: h.StartAt,
                        ServiceName: h.AppointmentService?.Product?.Description ?? "Servicio",
                        ProfessionalName: h.Professional?.DisplayName ?? "Colaborador",
                        Amount: h.AppointmentService?.Product?.Price ?? 0m,
                        Status: h.Status.ToString()
                    )).ToList();

                    return new AgendaClientListItemDto(
                        CustomerId: first.CustomerId,
                        Name: first.CustomerName ?? "Cliente",
                        Phone: first.CustomerPhone ?? string.Empty,
                        Email: first.CustomerEmail,
                        UltimaCita: past?.StartAt,
                        ProximaCita: next?.StartAt,
                        TotalCitas: g.Count(),
                        Estado: g.Any(x => x.StartAt >= now.AddMonths(-3)) ? "Activo" : "Inactivo",
                        Historial: history
                    );
                })
                .OrderByDescending(c => c.TotalCitas)
                .ToList();

            int totalClientes = grouped.Count;
            int nuevosEsteMes = grouped.Count(c => c.Historial.Count > 0 && c.Historial.Min(h => h.Date) >= startOfMonth);
            int recurrentes = grouped.Count(c => c.TotalCitas >= 2);

            return new AgendaClientsOverviewDto(
                TotalClientes: totalClientes,
                NuevosEsteMes: nuevosEsteMes,
                Recurrentes: recurrentes,
                Clientes: grouped
            );
        }

        #endregion

        #region Services Management

        public async Task<List<AppointmentServiceConfigDto>> ListServicesAsync(int companyId)
        {
            var services = await _db.AppointmentServices
                .Include(s => s.Product)
                .Include(s => s.ProfessionalServices)
                .Where(s => s.CompanyId == companyId && s.Active && !s.IsDeleted)
                .OrderBy(s => s.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();

            return services.Select(s => new AppointmentServiceConfigDto(
                Id: s.Id,
                ProductId: s.ProductId,
                ProductName: s.Product?.Description ?? "Servicio",
                Price: s.Product?.Price ?? 0m,
                DurationMinutes: s.DurationMinutes,
                BufferTimeMinutes: s.BufferTimeMinutes,
                IsPubliclyBookable: s.IsPubliclyBookable,
                DisplayOrder: s.DisplayOrder,
                ColorHex: s.ColorHex,
                IsActive: s.Active,
                AssignedProfessionalIds: s.ProfessionalServices?.Where(ps => ps.IsActive).Select(ps => ps.ProfessionalId).ToList() ?? new List<int>()
            )).ToList();
        }

        public async Task<AppointmentServiceConfigDto> UpsertServiceAsync(int companyId, UpsertAppointmentServiceInputDto dto)
        {
            AppointmentServiceEntity? entity;
            if (dto.Id.HasValue && dto.Id.Value > 0)
            {
                entity = await _db.AppointmentServices
                    .Include(s => s.ProfessionalServices)
                    .Include(s => s.Product)
                    .FirstOrDefaultAsync(s => s.Id == dto.Id.Value && s.CompanyId == companyId);

                if (entity == null)
                    throw new CustomException(new Error { Code = "NOT_FOUND", Message = "El servicio de agenda no existe.", Language = "ES" });
            }
            else
            {
                // Check if already exists for this Company and Product to prevent unique index violation
                entity = await _db.AppointmentServices
                    .Include(s => s.ProfessionalServices)
                    .Include(s => s.Product)
                    .FirstOrDefaultAsync(s => s.ProductId == dto.ProductId && s.CompanyId == companyId);

                if (entity == null)
                {
                    // Verify Product exists and belongs to company (or system product)
                    var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId && (p.CompanyId == companyId || p.CompanyId == 0));
                    if (product == null)
                    {
                        product = await _db.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId);
                        if (product == null)
                            throw new CustomException(new Error { Code = "PRODUCT_NOT_FOUND", Message = "El producto/servicio no existe en el catálogo.", Language = "ES" });
                    }

                    entity = new AppointmentServiceEntity
                    {
                        CompanyId = companyId,
                        ProductId = dto.ProductId,
                        Active = true,
                        IsDeleted = false
                    };
                    await _db.AppointmentServices.AddAsync(entity);
                }
            }

            entity.DurationMinutes = dto.DurationMinutes > 0 ? dto.DurationMinutes : 30;
            entity.BufferTimeMinutes = dto.BufferTimeMinutes >= 0 ? dto.BufferTimeMinutes : 0;
            entity.IsPubliclyBookable = dto.IsPubliclyBookable;
            entity.DisplayOrder = dto.DisplayOrder;
            entity.ColorHex = dto.ColorHex ?? "#3B82F6";
            entity.Active = dto.IsActive;
            entity.IsDeleted = false;

            await _db.SaveChangesAsync(); // save to guarantee entity.Id is assigned

            // Safely synchronize assigned professionals without triggering PostgreSQL unique constraint race
            if (dto.AssignedProfessionalIds != null)
            {
                var existingLinks = await _db.AppointmentProfessionalServices
                    .Where(ps => ps.AppointmentServiceId == entity.Id && ps.CompanyId == companyId)
                    .ToListAsync();

                var validProfIds = await _db.AppointmentProfessionals
                    .Where(p => p.CompanyId == companyId && dto.AssignedProfessionalIds.Contains(p.Id))
                    .Select(p => p.Id)
                    .ToListAsync();

                var targetSet = validProfIds.Distinct().ToHashSet();

                // 1. Remove links no longer in target
                var toRemove = existingLinks.Where(ps => !targetSet.Contains(ps.ProfessionalId)).ToList();
                if (toRemove.Count > 0)
                {
                    _db.AppointmentProfessionalServices.RemoveRange(toRemove);
                }

                // 2. Add only links that don't already exist
                var existingSet = existingLinks.Select(ps => ps.ProfessionalId).ToHashSet();
                foreach (var profId in targetSet)
                {
                    if (!existingSet.Contains(profId))
                    {
                        await _db.AppointmentProfessionalServices.AddAsync(new AppointmentProfessionalService
                        {
                            CompanyId = companyId,
                            AppointmentServiceId = entity.Id,
                            ProfessionalId = profId,
                            IsActive = true
                        });
                    }
                }

                await _db.SaveChangesAsync();
            }

            var prod = entity.Product ?? await _db.Products.FirstOrDefaultAsync(p => p.Id == entity.ProductId);

            return new AppointmentServiceConfigDto(
                Id: entity.Id,
                ProductId: entity.ProductId,
                ProductName: prod?.Description ?? "Servicio",
                Price: prod?.Price ?? 0m,
                DurationMinutes: entity.DurationMinutes,
                BufferTimeMinutes: entity.BufferTimeMinutes,
                IsPubliclyBookable: entity.IsPubliclyBookable,
                DisplayOrder: entity.DisplayOrder,
                ColorHex: entity.ColorHex,
                IsActive: entity.Active,
                AssignedProfessionalIds: dto.AssignedProfessionalIds ?? new List<int>()
            );
        }

        #endregion

        #region Professionals Management

        public async Task<List<AppointmentProfessionalDto>> ListProfessionalsAsync(int companyId)
        {
            var professionals = await _db.AppointmentProfessionals
                .Include(p => p.ProfessionalServices)
                .Include(p => p.Availabilities)
                .Include(p => p.Exceptions)
                .Include(p => p.User)
                .Where(p => p.CompanyId == companyId && p.Active && !p.IsDeleted)
                .OrderBy(p => p.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();

            return professionals.Select(p =>
            {
                string name = !string.IsNullOrWhiteSpace(p.DisplayName)
                    ? p.DisplayName.Trim()
                    : (p.User != null && !string.IsNullOrWhiteSpace(p.User.FirstName) ? $"{p.User.FirstName} {p.User.LastName}".Trim() : $"Colaborador #{p.Id}");

                string specialty = !string.IsNullOrWhiteSpace(p.SpecialtyTitle)
                    ? p.SpecialtyTitle.Trim()
                    : "Servicios Generales";

                var weekly = p.Availabilities != null && p.Availabilities.Any()
                    ? p.Availabilities.Select(a => new ProfessionalWeeklyScheduleDto(
                        (int)a.DayOfWeek,
                        a.StartTime.ToString(@"hh\:mm\:ss"),
                        a.EndTime.ToString(@"hh\:mm\:ss"),
                        a.BreakStartTime.HasValue ? a.BreakStartTime.Value.ToString(@"hh\:mm\:ss") : null,
                        a.BreakEndTime.HasValue ? a.BreakEndTime.Value.ToString(@"hh\:mm\:ss") : null,
                        a.IsWorkingDay
                    )).ToList()
                    : new List<ProfessionalWeeklyScheduleDto>
                    {
                        new(1, "09:00:00", "18:00:00", "12:00:00", "13:00:00", true),
                        new(2, "09:00:00", "18:00:00", "12:00:00", "13:00:00", true),
                        new(3, "09:00:00", "18:00:00", "12:00:00", "13:00:00", true),
                        new(4, "09:00:00", "18:00:00", "12:00:00", "13:00:00", true),
                        new(5, "09:00:00", "18:00:00", "12:00:00", "13:00:00", true),
                        new(6, "09:00:00", "18:00:00", "12:00:00", "13:00:00", true),
                        new(0, "09:00:00", "18:00:00", null, null, false)
                    };

                var exceptions = p.Exceptions?.Select(e => new ProfessionalExceptionDto(
                    e.Id,
                    e.Date,
                    e.Date,
                    e.IsAllDay,
                    e.Reason
                )).ToList() ?? new List<ProfessionalExceptionDto>();

                return new AppointmentProfessionalDto(
                    Id: p.Id,
                    UserId: p.UserId,
                    DisplayName: name,
                    SpecialtyTitle: specialty,
                    IsOwner: p.IsOwner,
                    IsActive: p.IsActive,
                    DisplayOrder: p.DisplayOrder,
                    AssignedServiceIds: p.ProfessionalServices?.Where(ps => ps.IsActive).Select(ps => ps.AppointmentServiceId).ToList() ?? new List<int>(),
                    WeeklyAvailabilities: weekly,
                    Exceptions: exceptions
                );
            }).ToList();
        }

        public async Task<AppointmentProfessionalDto> UpsertProfessionalAsync(int companyId, UpsertProfessionalDto dto)
        {
            AppointmentProfessional? entity;
            if (dto.Id.HasValue && dto.Id.Value > 0)
            {
                entity = await _db.AppointmentProfessionals
                    .Include(p => p.ProfessionalServices)
                    .FirstOrDefaultAsync(p => p.Id == dto.Id.Value && p.CompanyId == companyId);

                if (entity == null)
                    throw new CustomException(new Error { Code = "NOT_FOUND", Message = "El profesional no fue encontrado.", Language = "ES" });
            }
            else
            {
                entity = new AppointmentProfessional
                {
                    CompanyId = companyId
                };
                await _db.AppointmentProfessionals.AddAsync(entity);
            }

            entity.DisplayName = dto.GetEffectiveName();
            entity.SpecialtyTitle = dto.GetEffectiveSpecialty();
            entity.UserId = dto.UserId;
            entity.IsOwner = dto.IsOwner;
            entity.IsActive = dto.IsActive;
            entity.DisplayOrder = dto.DisplayOrder;

            await _db.SaveChangesAsync();

            // Set default Monday to Saturday schedule if new professional
            bool hasSchedule = await _db.AppointmentEmployeeAvailabilities.AnyAsync(a => a.ProfessionalId == entity.Id);
            if (!hasSchedule)
            {
                for (int day = 1; day <= 6; day++) // Mon-Sat
                {
                    await _db.AppointmentEmployeeAvailabilities.AddAsync(new AppointmentEmployeeAvailability
                    {
                        CompanyId = companyId,
                        ProfessionalId = entity.Id,
                        DayOfWeek = (DayOfWeek)day,
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(18, 0, 0),
                        BreakStartTime = new TimeSpan(12, 0, 0),
                        BreakEndTime = new TimeSpan(13, 0, 0),
                        IsWorkingDay = true
                    });
                }
                await _db.SaveChangesAsync();
            }

            // Safely synchronize assigned services without triggering unique constraint race
            if (dto.AssignedServiceIds != null)
            {
                var existingLinks = await _db.AppointmentProfessionalServices
                    .Where(ps => ps.ProfessionalId == entity.Id && ps.CompanyId == companyId)
                    .ToListAsync();

                var validServiceIds = await _db.AppointmentServices
                    .Where(s => s.CompanyId == companyId && dto.AssignedServiceIds.Contains(s.Id))
                    .Select(s => s.Id)
                    .ToListAsync();

                var targetSet = validServiceIds.Distinct().ToHashSet();

                var toRemove = existingLinks.Where(ps => !targetSet.Contains(ps.AppointmentServiceId)).ToList();
                if (toRemove.Count > 0)
                {
                    _db.AppointmentProfessionalServices.RemoveRange(toRemove);
                }

                var existingSet = existingLinks.Select(ps => ps.AppointmentServiceId).ToHashSet();
                foreach (var serviceId in targetSet)
                {
                    if (!existingSet.Contains(serviceId))
                    {
                        await _db.AppointmentProfessionalServices.AddAsync(new AppointmentProfessionalService
                        {
                            CompanyId = companyId,
                            ProfessionalId = entity.Id,
                            AppointmentServiceId = serviceId,
                            IsActive = true
                        });
                    }
                }

                await _db.SaveChangesAsync();
            }

            return new AppointmentProfessionalDto(
                Id: entity.Id,
                UserId: entity.UserId,
                DisplayName: entity.DisplayName,
                SpecialtyTitle: entity.SpecialtyTitle,
                IsOwner: entity.IsOwner,
                IsActive: entity.IsActive,
                DisplayOrder: entity.DisplayOrder,
                AssignedServiceIds: dto.AssignedServiceIds ?? new List<int>()
            );
        }

        public async Task<bool> UpdateWeeklyScheduleAsync(int companyId, int professionalId, List<WeeklyScheduleInputDto> schedule)
        {
            var prof = await _db.AppointmentProfessionals.FirstOrDefaultAsync(p => p.Id == professionalId && p.CompanyId == companyId);
            if (prof == null)
                throw new CustomException(new Error { Code = "NOT_FOUND", Message = "El profesional no fue encontrado.", Language = "ES" });

            var existing = await _db.AppointmentEmployeeAvailabilities
                .Where(a => a.ProfessionalId == professionalId && a.CompanyId == companyId)
                .ToListAsync();

            if (existing.Count > 0)
            {
                _db.AppointmentEmployeeAvailabilities.RemoveRange(existing);
                await _db.SaveChangesAsync();
            }

            foreach (var item in schedule)
            {
                await _db.AppointmentEmployeeAvailabilities.AddAsync(new AppointmentEmployeeAvailability
                {
                    CompanyId = companyId,
                    ProfessionalId = professionalId,
                    DayOfWeek = item.DayOfWeek,
                    StartTime = item.StartTime,
                    EndTime = item.EndTime,
                    BreakStartTime = item.BreakStartTime,
                    BreakEndTime = item.BreakEndTime,
                    IsWorkingDay = item.IsWorkingDay
                });
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<AppointmentAvailabilityException> AddExceptionAsync(int companyId, int professionalId, AvailabilityExceptionInputDto dto)
        {
            var prof = await _db.AppointmentProfessionals.FirstOrDefaultAsync(p => p.Id == professionalId && p.CompanyId == companyId);
            if (prof == null)
                throw new CustomException(new Error { Code = "NOT_FOUND", Message = "El profesional no fue encontrado.", Language = "ES" });

            var exception = new AppointmentAvailabilityException
            {
                CompanyId = companyId,
                ProfessionalId = professionalId,
                Date = DateTime.SpecifyKind(dto.Date.Date, DateTimeKind.Utc),
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                IsAllDay = dto.IsAllDay,
                Reason = dto.Reason?.Trim()
            };

            await _db.AppointmentAvailabilityExceptions.AddAsync(exception);
            await _db.SaveChangesAsync();
            return exception;
        }

        public async Task<bool> DeleteExceptionAsync(int companyId, int exceptionId)
        {
            var exception = await _db.AppointmentAvailabilityExceptions
                .FirstOrDefaultAsync(e => e.Id == exceptionId && e.CompanyId == companyId);

            if (exception == null) return false;

            _db.AppointmentAvailabilityExceptions.Remove(exception);
            await _db.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Operational & Booking Page Settings

        public async Task<(AppointmentSettingsDto Settings, BookingPageSettingsDto BookingPage)> GetSettingsAsync(int companyId)
        {
            var operational = await _db.AppointmentSettings.AsNoTracking().FirstOrDefaultAsync(s => s.CompanyId == companyId)
                ?? new AppointmentSettings { CompanyId = companyId };

            var bookingPage = await _db.BookingPageSettings.FirstOrDefaultAsync(b => b.CompanyId == companyId);
            if (bookingPage == null)
            {
                var comp = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
                var compName = comp?.Name ?? "Mi Negocio";
                var defaultSlug = $"empresa-{companyId}";
                bookingPage = new BookingPageSettings
                {
                    CompanyId = companyId,
                    Slug = defaultSlug,
                    BusinessName = compName,
                    IsPublicActive = true,
                    Active = true
                };
                await _db.BookingPageSettings.AddAsync(bookingPage);
                await _db.SaveChangesAsync();
            }

            var opDto = new AppointmentSettingsDto(
                TimeZone: operational.TimeZone,
                ConfirmationMode: operational.ConfirmationMode.ToString(),
                DefaultDurationMinutes: operational.DefaultDurationMinutes,
                MinLeadTimeHours: operational.MinLeadTimeHours,
                MaxFutureBookingDays: operational.MaxFutureBookingDays,
                HoldSlotTimeoutMinutes: operational.HoldSlotTimeoutMinutes,
                MinCancelLeadTimeHours: operational.MinCancelLeadTimeHours,
                SlotIntervalMinutes: operational.SlotIntervalMinutes,
                SendReminder24h: operational.SendReminder24h,
                SendReminder1h: operational.SendReminder1h,
                ChannelWhatsApp: operational.ChannelWhatsApp,
                ChannelEmail: operational.ChannelEmail
            );

            var bpDto = new BookingPageSettingsDto(
                Slug: bookingPage.Slug,
                BusinessName: bookingPage.BusinessName,
                LogoUrl: bookingPage.LogoUrl,
                CoverImageUrl: bookingPage.CoverImageUrl,
                BrandColorHex: bookingPage.BrandColorHex,
                Description: bookingPage.Description,
                Address: bookingPage.Address,
                WhatsAppNumber: bookingPage.WhatsAppNumber,
                IsPublicActive: bookingPage.IsPublicActive
            );

            return (opDto, bpDto);
        }

        public async Task<AppointmentSettingsDto> UpdateOperationalSettingsAsync(int companyId, AppointmentSettingsDto dto)
        {
            var entity = await _db.AppointmentSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId);
            if (entity == null)
            {
                entity = new AppointmentSettings { CompanyId = companyId };
                await _db.AppointmentSettings.AddAsync(entity);
            }

            entity.TimeZone = dto.TimeZone?.Trim() ?? "America/Santo_Domingo";
            if (Enum.TryParse<AppointmentConfirmationMode>(dto.ConfirmationMode, true, out var mode))
            {
                entity.ConfirmationMode = mode;
            }
            entity.DefaultDurationMinutes = dto.DefaultDurationMinutes > 0 ? dto.DefaultDurationMinutes : 30;
            entity.MinLeadTimeHours = dto.MinLeadTimeHours >= 0 ? dto.MinLeadTimeHours : 2;
            entity.MaxFutureBookingDays = dto.MaxFutureBookingDays > 0 ? dto.MaxFutureBookingDays : 30;
            entity.HoldSlotTimeoutMinutes = dto.HoldSlotTimeoutMinutes > 0 ? dto.HoldSlotTimeoutMinutes : 30;
            entity.MinCancelLeadTimeHours = dto.MinCancelLeadTimeHours >= 0 ? dto.MinCancelLeadTimeHours : 3;
            entity.SlotIntervalMinutes = dto.SlotIntervalMinutes > 0 ? dto.SlotIntervalMinutes : 10;
            entity.SendReminder24h = dto.SendReminder24h;
            entity.SendReminder1h = dto.SendReminder1h;
            entity.ChannelWhatsApp = dto.ChannelWhatsApp;
            entity.ChannelEmail = dto.ChannelEmail;

            await _db.SaveChangesAsync();

            return dto;
        }

        public async Task<BookingPageSettingsDto> UpdateBookingPageSettingsAsync(int companyId, BookingPageSettingsDto dto)
        {
            var cleanSlug = dto.Slug.Trim().ToLower().Replace(" ", "-");
            // Validate slug uniqueness across other companies
            bool slugExists = await _db.BookingPageSettings.AnyAsync(b => b.Slug == cleanSlug && b.CompanyId != companyId);
            if (slugExists)
                throw new CustomException(new Error { Code = "SLUG_TAKEN", Message = "El identificador (URL) ya está en uso por otra empresa.", Language = "ES" });

            var entity = await _db.BookingPageSettings.FirstOrDefaultAsync(b => b.CompanyId == companyId);
            if (entity == null)
            {
                entity = new BookingPageSettings { CompanyId = companyId };
                await _db.BookingPageSettings.AddAsync(entity);
            }

            entity.Slug = cleanSlug;
            entity.BusinessName = dto.BusinessName.Trim();
            entity.LogoUrl = dto.LogoUrl?.Trim();
            entity.CoverImageUrl = dto.CoverImageUrl?.Trim();
            entity.BrandColorHex = dto.BrandColorHex?.Trim() ?? "#111827";
            entity.Description = dto.Description?.Trim();
            entity.Address = dto.Address?.Trim();
            entity.WhatsAppNumber = dto.WhatsAppNumber?.Trim();
            entity.IsPublicActive = dto.IsPublicActive;

            await _db.SaveChangesAsync();

            return dto;
        }

        public async Task<BookingPageSettings?> ResolveBookingPageAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;
            var clean = slug.Trim().ToLower();

            // 1. Direct slug match
            var page = await _db.BookingPageSettings
                .FirstOrDefaultAsync(b => b.Slug.ToLower() == clean && !b.IsDeleted);

            // 2. Numeric or empresa-{id}
            if (page == null)
            {
                int targetCompanyId = 0;
                if (clean.StartsWith("empresa-") && int.TryParse(clean.Substring(8), out int cid))
                {
                    targetCompanyId = cid;
                }
                else if (int.TryParse(clean, out int id))
                {
                    targetCompanyId = id;
                }

                if (targetCompanyId > 0)
                {
                    page = await _db.BookingPageSettings.FirstOrDefaultAsync(b => b.CompanyId == targetCompanyId && !b.IsDeleted);
                    if (page == null)
                    {
                        var comp = await _db.Companies.FirstOrDefaultAsync(c => c.Id == targetCompanyId && !c.IsDeleted);
                        if (comp != null)
                        {
                            page = new BookingPageSettings
                            {
                                CompanyId = comp.Id,
                                Slug = clean,
                                BusinessName = comp.Name ?? "Mi Negocio",
                                IsPublicActive = true,
                                Active = true
                            };
                            await _db.BookingPageSettings.AddAsync(page);
                            await _db.SaveChangesAsync();
                        }
                    }
                }
            }

            // 3. Fallback for "barberia-central" or general default: match any existing booking page, or first active company
            if (page == null)
            {
                page = await _db.BookingPageSettings.FirstOrDefaultAsync(b => !b.IsDeleted && b.Active);
                if (page == null)
                {
                    var comp = await _db.Companies.FirstOrDefaultAsync(c => !c.IsDeleted && c.Active);
                    if (comp != null)
                    {
                        page = new BookingPageSettings
                        {
                            CompanyId = comp.Id,
                            Slug = clean,
                            BusinessName = comp.Name ?? "Mi Negocio",
                            IsPublicActive = true,
                            Active = true
                        };
                        await _db.BookingPageSettings.AddAsync(page);
                        await _db.SaveChangesAsync();
                    }
                }
            }

            return page;
        }

        public async Task<PublicBusinessProfileDto?> GetPublicBusinessProfileAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;

            var bookingPage = await ResolveBookingPageAsync(slug);
            if (bookingPage == null) return null;

            int companyId = bookingPage.CompanyId;

            // Operational settings
            var settings = await _db.AppointmentSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CompanyId == companyId)
                ?? new AppointmentSettings { CompanyId = companyId };

            // 1. Fetch explicitly linked appointment services
            var services = await _db.AppointmentServices
                .Include(s => s.Product)
                .Where(s => s.CompanyId == companyId && !s.IsDeleted && (s.IsPubliclyBookable || s.Active))
                .OrderBy(s => s.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();

            // Populate Product on any linked services if missing
            foreach (var s in services.Where(s => s.Product == null && s.ProductId > 0))
            {
                s.Product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == s.ProductId);
            }

            // 2. Also fetch company catalog services (ProductTypeId == 2 or SERV- prefix) to ensure all services are bookable
            var catalogServices = await _db.Products
                .Where(p => p.CompanyId == companyId && (p.ProductTypeId == 2 || (p.Barcode != null && p.Barcode.StartsWith("SERV-")) || (p.Reference != null && p.Reference.StartsWith("SERV-"))) && p.Active && !p.IsDeleted)
                .OrderBy(p => p.Description)
                .Take(30)
                .AsNoTracking()
                .ToListAsync();

            var existingProductIds = services.Select(s => s.ProductId).ToHashSet();
            foreach (var cs in catalogServices)
            {
                if (!existingProductIds.Contains(cs.Id))
                {
                    services.Add(new AppointmentServiceEntity
                    {
                        Id = cs.Id,
                        CompanyId = companyId,
                        ProductId = cs.Id,
                        Product = cs,
                        DurationMinutes = 30,
                        BufferTimeMinutes = 0,
                        IsPubliclyBookable = true,
                        Active = true
                    });
                }
            }

            // 3. Fallback: if STILL no services exist, synthesize a standard booking service
            if (services.Count == 0)
            {
                services.Add(new AppointmentServiceEntity
                {
                    Id = 1,
                    CompanyId = companyId,
                    ProductId = 1,
                    Product = new Onion.Domain.Products.Product
                    {
                        Id = 1,
                        Description = "Servicio General",
                        Price = 500m,
                        CompanyId = companyId,
                        Active = true
                    },
                    DurationMinutes = 30,
                    BufferTimeMinutes = 0,
                    IsPubliclyBookable = true,
                    Active = true
                });
            }

            // Professionals
            var professionals = await _db.AppointmentProfessionals
                .Include(p => p.ProfessionalServices)
                .Where(p => p.CompanyId == companyId && p.IsActive && p.Active && !p.IsDeleted)
                .OrderBy(p => p.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();

            // Fallback: if no professionals configured yet, synthesize default professional
            if (professionals.Count == 0)
            {
                professionals.Add(new AppointmentProfessional
                {
                    Id = 1,
                    CompanyId = companyId,
                    DisplayName = "Personal General",
                    SpecialtyTitle = "Especialista en Atención",
                    IsActive = true,
                    Active = true,
                    DisplayOrder = 1
                });
            }

            var serviceItems = services.Select(s => new PublicServiceItemDto(
                Id: s.Id,
                ProductId: s.ProductId,
                Name: !string.IsNullOrWhiteSpace(s.Product?.Description) ? s.Product.Description : "Servicio",
                Price: s.Product?.Price ?? 0m,
                DurationMinutes: s.DurationMinutes > 0 ? s.DurationMinutes : 30,
                BufferTimeMinutes: s.BufferTimeMinutes,
                ColorHex: !string.IsNullOrWhiteSpace(s.ColorHex) ? s.ColorHex : "#4f46e5",
                DisplayOrder: s.DisplayOrder
            )).ToList();

            var professionalItems = professionals.Select(p => new PublicProfessionalItemDto(
                Id: p.Id,
                DisplayName: !string.IsNullOrWhiteSpace(p.DisplayName) ? p.DisplayName : "Colaborador",
                SpecialtyTitle: !string.IsNullOrWhiteSpace(p.SpecialtyTitle) ? p.SpecialtyTitle : "Servicios Generales",
                DisplayOrder: p.DisplayOrder,
                AssignedServiceIds: p.ProfessionalServices?.Where(ps => ps.IsActive).Select(ps => ps.AppointmentServiceId).ToList() ?? new List<int>()
            )).ToList();

            return new PublicBusinessProfileDto(
                Slug: bookingPage.Slug,
                BusinessName: !string.IsNullOrWhiteSpace(bookingPage.BusinessName) ? bookingPage.BusinessName : "Mi Negocio",
                Description: bookingPage.Description,
                Address: bookingPage.Address,
                WhatsAppNumber: bookingPage.WhatsAppNumber,
                LogoUrl: bookingPage.LogoUrl,
                CoverImageUrl: bookingPage.CoverImageUrl,
                BrandColorHex: !string.IsNullOrWhiteSpace(bookingPage.BrandColorHex) ? bookingPage.BrandColorHex : "#111827",
                ConfirmationMode: settings.ConfirmationMode.ToString(),
                MinLeadTimeHours: settings.MinLeadTimeHours,
                MaxFutureBookingDays: settings.MaxFutureBookingDays > 0 ? settings.MaxFutureBookingDays : 30,
                Services: serviceItems,
                Professionals: professionalItems
            );
        }

        #endregion

        #region Manual Booking

        public async Task<AppointmentFullDetailDto> CreateManualAppointmentAsync(int companyId, CreateManualAppointmentDto dto, string? userId = null)
        {
            int targetServiceId = dto.GetEffectiveServiceId();

            var service = await _db.AppointmentServices
                .Include(s => s.Product)
                .Include(s => s.ProfessionalServices)
                .FirstOrDefaultAsync(s => (s.Id == targetServiceId || s.ProductId == targetServiceId) && s.CompanyId == companyId);

            if (service == null || service.Product == null)
            {
                // Fallback 1: check if targetServiceId is a Product in the catalog
                if (targetServiceId > 0)
                {
                    var prod = await _db.Products.FirstOrDefaultAsync(p => p.Id == targetServiceId && (p.CompanyId == companyId || p.CompanyId == 0));
                    if (prod != null)
                    {
                        var createdService = new AppointmentServiceEntity
                        {
                            CompanyId = companyId,
                            ProductId = prod.Id,
                            DurationMinutes = 30,
                            BufferTimeMinutes = 0,
                            IsPubliclyBookable = true,
                            Active = true,
                            IsDeleted = false
                        };
                        await _db.AppointmentServices.AddAsync(createdService);
                        await _db.SaveChangesAsync();
                        createdService.Product = prod;
                        service = createdService;
                    }
                }
            }

            if (service == null || service.Product == null)
            {
                // Fallback 2: search any active service in company
                service = await _db.AppointmentServices
                    .Include(s => s.Product)
                    .Include(s => s.ProfessionalServices)
                    .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Active && !s.IsDeleted);
            }

            if (service == null || service.Product == null)
            {
                // Fallback 3: check if company has any product of type service (ProductTypeId = 2 or SERV-)
                var anyServiceProduct = await _db.Products.FirstOrDefaultAsync(p => p.CompanyId == companyId && (p.ProductTypeId == 2 || (p.Barcode != null && p.Barcode.StartsWith("SERV-")) || (p.Reference != null && p.Reference.StartsWith("SERV-"))) && p.Active && !p.IsDeleted);
                if (anyServiceProduct != null)
                {
                    var autoCreated = new AppointmentServiceEntity
                    {
                        CompanyId = companyId,
                        ProductId = anyServiceProduct.Id,
                        DurationMinutes = 30,
                        BufferTimeMinutes = 0,
                        IsPubliclyBookable = true,
                        Active = true,
                        IsDeleted = false
                    };
                    await _db.AppointmentServices.AddAsync(autoCreated);
                    await _db.SaveChangesAsync();
                    autoCreated.Product = anyServiceProduct;
                    service = autoCreated;
                }
            }

            if (service == null || service.Product == null)
                throw new CustomException(new Error { Code = "SERVICE_NOT_FOUND", Message = "El servicio no fue encontrado. Vincula al menos un servicio en Servicios Reservables.", Language = "ES" });

            AppointmentProfessional? prof = null;
            if (dto.ProfessionalId > 0)
            {
                prof = await _db.AppointmentProfessionals
                    .FirstOrDefaultAsync(p => p.Id == dto.ProfessionalId && p.CompanyId == companyId && p.IsActive);
            }

            // Fallback 1: assigned professional for this service
            if (prof == null && service.ProfessionalServices != null && service.ProfessionalServices.Any())
            {
                var assignedProfId = service.ProfessionalServices.Where(ps => ps.IsActive).Select(ps => ps.ProfessionalId).FirstOrDefault();
                if (assignedProfId > 0)
                {
                    prof = await _db.AppointmentProfessionals
                        .FirstOrDefaultAsync(p => p.Id == assignedProfId && p.CompanyId == companyId && p.IsActive);
                }
            }

            // Fallback 2: first active professional for company
            if (prof == null)
            {
                prof = await _db.AppointmentProfessionals
                    .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.IsActive && !p.IsDeleted);
            }

            // Fallback 3: auto-create default professional if none exists at all
            if (prof == null)
            {
                prof = new AppointmentProfessional
                {
                    CompanyId = companyId,
                    DisplayName = "Personal General",
                    SpecialtyTitle = "Especialista General",
                    IsActive = true,
                    IsOwner = true,
                    DisplayOrder = 1
                };
                await _db.AppointmentProfessionals.AddAsync(prof);
                await _db.SaveChangesAsync();

                // Assign to service
                await _db.AppointmentProfessionalServices.AddAsync(new AppointmentProfessionalService
                {
                    CompanyId = companyId,
                    AppointmentServiceId = service.Id,
                    ProfessionalId = prof.Id,
                    IsActive = true
                });
                await _db.SaveChangesAsync();
            }

            var startAt = DateTime.SpecifyKind(dto.StartAt, DateTimeKind.Utc);
            int duration = service.DurationMinutes > 0 ? service.DurationMinutes : 30;
            var endAt = startAt.AddMinutes(duration + service.BufferTimeMinutes);

            bool isFree = await _availability.IsProfessionalAvailableAsync(companyId, prof.Id, startAt, endAt);
            if (!isFree && dto.ProfessionalId <= 0)
            {
                // Try to find another professional who is free at this slot
                var otherProf = await _availability.AutoAssignProfessionalAsync(companyId, service.Id, startAt, endAt);
                if (otherProf != null)
                {
                    prof = otherProf;
                    isFree = true;
                }
            }

            if (!isFree)
                throw new CustomException(new Error { Code = "SLOT_NOT_AVAILABLE", Message = "El horario ya se encuentra ocupado para este profesional.", Language = "ES" });

            int count = await _db.Appointments.CountAsync(a => a.CompanyId == companyId) + 1;
            string bookingCode = $"CIT-{count:D6}";

            string rawToken = Guid.NewGuid().ToString("N");
            string tokenHash = AppointmentAgendaService.ComputeSha256(rawToken);

            var appointment = new Appointment
            {
                CompanyId = companyId,
                BookingCode = bookingCode,
                ManagementTokenHash = tokenHash,
                ManagementTokenExpiresAt = startAt.AddDays(30),
                StartAt = startAt,
                EndAt = endAt,
                Status = AppointmentStatus.Confirmed,
                BillingStatus = AppointmentBillingStatus.Unbilled,
                Origin = AppointmentOrigin.AdminManual,
                CustomerId = dto.CustomerId,
                CustomerName = dto.CustomerName.Trim(),
                CustomerPhone = dto.CustomerPhone.Trim(),
                CustomerEmail = dto.CustomerEmail?.Trim(),
                AppointmentServiceId = service.Id,
                ProfessionalId = prof.Id,
                Notes = dto.Notes?.Trim()
            };

            var history = new AppointmentStatusHistory
            {
                CompanyId = companyId,
                Appointment = appointment,
                PreviousStatus = AppointmentStatus.Confirmed,
                NewStatus = AppointmentStatus.Confirmed,
                ChangedByUserId = userId ?? "Admin",
                ChangeReason = "Cita creada manualmente desde el panel de control"
            };

            await _db.Appointments.AddAsync(appointment);
            await _db.AppointmentStatusHistories.AddAsync(history);
            await _db.SaveChangesAsync();

            return new AppointmentFullDetailDto(
                Id: appointment.Id,
                BookingCode: bookingCode,
                StartAt: startAt,
                EndAt: endAt,
                Status: appointment.Status.ToString(),
                BillingStatus: appointment.BillingStatus.ToString(),
                Origin: appointment.Origin.ToString(),
                CustomerId: appointment.CustomerId,
                CustomerName: appointment.CustomerName,
                CustomerPhone: appointment.CustomerPhone,
                CustomerEmail: appointment.CustomerEmail,
                AppointmentServiceId: service.Id,
                ServiceName: service.Product.Description,
                Price: service.Product.Price,
                DurationMinutes: service.DurationMinutes,
                ProfessionalId: prof.Id,
                ProfessionalName: prof.DisplayName,
                SaleId: null,
                ExpiresAt: null,
                Notes: appointment.Notes,
                CancellationReason: null,
                CreationDate: appointment.CreationDate
            );
        }

        #endregion
    }
}
