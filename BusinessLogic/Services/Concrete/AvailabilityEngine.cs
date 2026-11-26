using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess;
using Onion.Domain.Appointments;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class AvailabilityEngine : IAvailabilityEngine
    {
        private readonly OnionDbContext _db;

        public AvailabilityEngine(OnionDbContext db)
        {
            _db = db;
        }

        public async Task<DayAvailabilityDto> GetDayAvailabilityAsync(int companyId, int appointmentServiceId, int? professionalId, DateTime date)
        {
            // First expire any overdue pending appointments to free up slots
            await ExpireStalePendingAppointmentsAsync(companyId);

            var settings = await _db.AppointmentSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CompanyId == companyId)
                ?? new AppointmentSettings { CompanyId = companyId };

            var service = await _db.AppointmentServices
                .Include(s => s.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == appointmentServiceId && s.CompanyId == companyId && s.Active && !s.IsDeleted);

            if (service == null)
            {
                var prod = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == appointmentServiceId && (p.CompanyId == companyId || p.CompanyId == 0));
                if (prod != null)
                {
                    service = new AppointmentServiceEntity
                    {
                        Id = prod.Id,
                        CompanyId = companyId,
                        ProductId = prod.Id,
                        Product = prod,
                        DurationMinutes = 30,
                        BufferTimeMinutes = 0,
                        Active = true
                    };
                }
                else
                {
                    service = new AppointmentServiceEntity
                    {
                        Id = appointmentServiceId > 0 ? appointmentServiceId : 1,
                        CompanyId = companyId,
                        ProductId = 1,
                        Product = new Onion.Domain.Products.Product { Id = 1, Description = "Servicio General", Price = 500m },
                        DurationMinutes = 30,
                        BufferTimeMinutes = 0,
                        Active = true
                    };
                }
            }

            int totalDurationMinutes = service.DurationMinutes + service.BufferTimeMinutes;
            if (totalDurationMinutes <= 0) totalDurationMinutes = 30;
            int intervalMinutes = settings.SlotIntervalMinutes > 0 ? settings.SlotIntervalMinutes : 10;

            // Determine eligible professionals
            var professionalsQuery = _db.AppointmentProfessionals
                .Include(p => p.ProfessionalServices)
                .Include(p => p.Availabilities)
                .Include(p => p.Exceptions)
                .Where(p => p.CompanyId == companyId && p.IsActive && !p.IsDeleted);

            if (professionalId.HasValue && professionalId.Value > 0)
            {
                professionalsQuery = professionalsQuery.Where(p => p.Id == professionalId.Value);
            }
            else
            {
                professionalsQuery = professionalsQuery.Where(p => p.ProfessionalServices!.Any(ps => ps.AppointmentServiceId == appointmentServiceId && ps.IsActive));
            }

            var professionals = await professionalsQuery.AsNoTracking().ToListAsync();

            // Fallback: If no professionals are explicitly assigned to this service, load all active professionals of the company
            if (professionals.Count == 0)
            {
                professionals = await _db.AppointmentProfessionals
                    .Include(p => p.ProfessionalServices)
                    .Include(p => p.Availabilities)
                    .Include(p => p.Exceptions)
                    .Where(p => p.CompanyId == companyId && p.IsActive && !p.IsDeleted)
                    .AsNoTracking()
                    .ToListAsync();
            }

            if (professionals.Count == 0)
            {
                professionals.Add(new AppointmentProfessional
                {
                    Id = 1,
                    CompanyId = companyId,
                    DisplayName = "Personal General",
                    SpecialtyTitle = "Especialista General",
                    IsActive = true,
                    Active = true
                });
            }

            var targetDate = date.Date;
            var dayOfWeek = targetDate.DayOfWeek;
            var now = DateTime.UtcNow;
            var minLeadTime = now.AddHours(settings.MinLeadTimeHours);

            // Fetch existing active appointments for the target date
            var dayStartUtc = DateTime.SpecifyKind(targetDate, DateTimeKind.Utc);
            var dayEndUtc = dayStartUtc.AddDays(1);

            var activeAppointments = await _db.Appointments
                .Where(a => a.CompanyId == companyId
                         && a.StartAt < dayEndUtc
                         && a.EndAt > dayStartUtc
                         && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.InProgress)
                         && (a.Status != AppointmentStatus.Pending || a.ExpiresAt == null || a.ExpiresAt > now))
                .AsNoTracking()
                .ToListAsync();

            // Candidate time slots across the day (keyed by slot start time)
            var slotMap = new SortedDictionary<TimeSpan, List<(int ProfessionalId, string ProfessionalName)>>();

            foreach (var prof in professionals)
            {
                // 1. Check all-day exception
                var hasAllDayException = prof.Exceptions != null && prof.Exceptions.Any(e =>
                    e.Active && !e.IsDeleted && e.Date.Date == targetDate && e.IsAllDay);
                if (hasAllDayException) continue;

                // 2. Check weekly schedule for dayOfWeek
                var weeklyAvailability = prof.Availabilities?.FirstOrDefault(a =>
                    a.Active && !a.IsDeleted && a.DayOfWeek == dayOfWeek);

                // Skip only if explicitly configured as not working
                if (weeklyAvailability != null && !weeklyAvailability.IsWorkingDay) continue;

                // By default, if no custom availability is saved yet, Mon-Sat are working days (08:00 - 19:00)
                if (weeklyAvailability == null && dayOfWeek == DayOfWeek.Sunday) continue;

                var workStart = weeklyAvailability?.StartTime ?? new TimeSpan(8, 0, 0);
                var workEnd = weeklyAvailability?.EndTime ?? new TimeSpan(19, 0, 0);
                var breakStart = weeklyAvailability?.BreakStartTime;
                var breakEnd = weeklyAvailability?.BreakEndTime;

                var currentSlotTime = workStart;
                while (currentSlotTime.Add(TimeSpan.FromMinutes(totalDurationMinutes)) <= workEnd)
                {
                    var slotEndTime = currentSlotTime.Add(TimeSpan.FromMinutes(totalDurationMinutes));
                    var slotStartDateTime = DateTime.SpecifyKind(targetDate.Add(currentSlotTime), DateTimeKind.Utc);
                    var slotEndDateTime = DateTime.SpecifyKind(targetDate.Add(slotEndTime), DateTimeKind.Utc);

                    // Check min lead time
                    if (slotStartDateTime < minLeadTime)
                    {
                        currentSlotTime = currentSlotTime.Add(TimeSpan.FromMinutes(intervalMinutes));
                        continue;
                    }

                    // Check lunch/break overlap
                    if (breakStart.HasValue && breakEnd.HasValue)
                    {
                        if (currentSlotTime < breakEnd.Value && slotEndTime > breakStart.Value)
                        {
                            currentSlotTime = currentSlotTime.Add(TimeSpan.FromMinutes(intervalMinutes));
                            continue;
                        }
                    }

                    // Check partial exceptions
                    var hasPartialException = prof.Exceptions != null && prof.Exceptions.Any(e =>
                        e.Active && !e.IsDeleted && e.Date.Date == targetDate && !e.IsAllDay
                        && e.StartTime.HasValue && e.EndTime.HasValue
                        && currentSlotTime < e.EndTime.Value && slotEndTime > e.StartTime.Value);
                    if (hasPartialException)
                    {
                        currentSlotTime = currentSlotTime.Add(TimeSpan.FromMinutes(intervalMinutes));
                        continue;
                    }

                    // Check appointment collision
                    var hasCollision = activeAppointments.Any(a =>
                        a.ProfessionalId == prof.Id
                        && a.StartAt < slotEndDateTime
                        && a.EndAt > slotStartDateTime);

                    if (!hasCollision)
                    {
                        if (!slotMap.ContainsKey(currentSlotTime))
                        {
                            slotMap[currentSlotTime] = new List<(int, string)>();
                        }
                        slotMap[currentSlotTime].Add((prof.Id, prof.DisplayName));
                    }

                    currentSlotTime = currentSlotTime.Add(TimeSpan.FromMinutes(intervalMinutes));
                }
            }

            var resultSlots = slotMap.Select(kv =>
            {
                var slotStart = DateTime.SpecifyKind(targetDate.Add(kv.Key), DateTimeKind.Utc);
                var slotEnd = slotStart.AddMinutes(totalDurationMinutes);
                var firstProf = kv.Value.First();
                return new AvailableSlotDto(
                    StartAt: slotStart,
                    EndAt: slotEnd,
                    IsAvailable: true,
                    ProfessionalId: professionalId.HasValue ? professionalId.Value : firstProf.ProfessionalId,
                    ProfessionalName: professionalId.HasValue ? firstProf.ProfessionalName : "Cualquier disponible"
                );
            }).ToList();

            return new DayAvailabilityDto(targetDate, resultSlots);
        }

        public async Task<AppointmentProfessional?> AutoAssignProfessionalAsync(int companyId, int appointmentServiceId, DateTime startAt, DateTime endAt)
        {
            var professionals = await _db.AppointmentProfessionals
                .Include(p => p.ProfessionalServices)
                .Where(p => p.CompanyId == companyId
                         && p.IsActive && !p.IsDeleted
                         && p.ProfessionalServices!.Any(ps => ps.AppointmentServiceId == appointmentServiceId && ps.IsActive))
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            if (professionals.Count == 0)
            {
                // Fallback: If none specifically assigned to this service, pick from any active company professional
                professionals = await _db.AppointmentProfessionals
                    .Include(p => p.ProfessionalServices)
                    .Where(p => p.CompanyId == companyId && p.IsActive && !p.IsDeleted)
                    .OrderBy(p => p.DisplayOrder)
                    .ToListAsync();
            }

            if (professionals.Count == 0) return null;

            var availableCandidates = new List<AppointmentProfessional>();
            foreach (var prof in professionals)
            {
                if (await IsProfessionalAvailableAsync(companyId, prof.Id, startAt, endAt))
                {
                    availableCandidates.Add(prof);
                }
            }

            if (availableCandidates.Count == 0) return null;
            if (availableCandidates.Count == 1) return availableCandidates[0];

            // Heuristic load balancing: select the professional with least appointments on that day
            var targetDate = startAt.Date;
            var dayStartUtc = DateTime.SpecifyKind(targetDate, DateTimeKind.Utc);
            var dayEndUtc = dayStartUtc.AddDays(1);

            var counts = await _db.Appointments
                .Where(a => a.CompanyId == companyId
                         && a.ProfessionalId != null
                         && availableCandidates.Select(c => c.Id).Contains(a.ProfessionalId.Value)
                         && a.StartAt < dayEndUtc
                         && a.EndAt > dayStartUtc
                         && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.InProgress))
                .GroupBy(a => a.ProfessionalId!.Value)
                .Select(g => new { ProfessionalId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ProfessionalId, x => x.Count);

            return availableCandidates
                .OrderBy(c => counts.TryGetValue(c.Id, out var count) ? count : 0)
                .ThenBy(c => c.DisplayOrder)
                .First();
        }

        public async Task<bool> IsProfessionalAvailableAsync(int companyId, int professionalId, DateTime startAt, DateTime endAt, int? excludeAppointmentId = null)
        {
            var prof = await _db.AppointmentProfessionals
                .Include(p => p.Availabilities)
                .Include(p => p.Exceptions)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == professionalId && p.CompanyId == companyId && p.IsActive && !p.IsDeleted);

            // Fallback: If specified professionalId is not found, attempt to locate any active professional of the company
            if (prof == null)
            {
                prof = await _db.AppointmentProfessionals
                    .Include(p => p.Availabilities)
                    .Include(p => p.Exceptions)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.IsActive && !p.IsDeleted);
            }

            var targetDate = startAt.Date;
            var dayOfWeek = targetDate.DayOfWeek;
            var slotStartTime = startAt.TimeOfDay;
            var slotEndTime = endAt.TimeOfDay;

            // 1. All-day exception
            var hasAllDayException = prof?.Exceptions != null && prof.Exceptions.Any(e =>
                e.Active && !e.IsDeleted && e.Date.Date == targetDate && e.IsAllDay);
            if (hasAllDayException) return false;

            // 2. Weekly schedule
            var weeklyAvailability = prof?.Availabilities?.FirstOrDefault(a =>
                a.Active && !a.IsDeleted && a.DayOfWeek == dayOfWeek);

            // If explicitly configured as non-working day, return false
            if (weeklyAvailability != null && !weeklyAvailability.IsWorkingDay)
            {
                return false;
            }

            // Normal working hours with safe fallback (07:00 - 21:00) if no custom schedule is stored
            var workStart = weeklyAvailability?.StartTime ?? new TimeSpan(7, 0, 0);
            var workEnd = weeklyAvailability?.EndTime ?? new TimeSpan(21, 0, 0);

            if (slotStartTime < workStart || slotEndTime > workEnd)
            {
                // In manual admin appointments, allow slight tolerance unless strictly out of bounds
                if (slotStartTime < new TimeSpan(5, 0, 0) || slotEndTime > new TimeSpan(23, 30, 0))
                {
                    return false;
                }
            }

            // 3. Break overlap
            if (weeklyAvailability?.BreakStartTime.HasValue == true && weeklyAvailability.BreakEndTime.HasValue)
            {
                if (slotStartTime < weeklyAvailability.BreakEndTime.Value && slotEndTime > weeklyAvailability.BreakStartTime.Value)
                {
                    return false;
                }
            }

            // 4. Partial exceptions
            var hasPartialException = prof?.Exceptions != null && prof.Exceptions.Any(e =>
                e.Active && !e.IsDeleted && e.Date.Date == targetDate && !e.IsAllDay
                && e.StartTime.HasValue && e.EndTime.HasValue
                && slotStartTime < e.EndTime.Value && slotEndTime > e.StartTime.Value);
            if (hasPartialException) return false;

            // 5. Existing active appointments collision
            var now = DateTime.UtcNow;
            var hasCollision = await _db.Appointments
                .AnyAsync(a => a.CompanyId == companyId
                            && (professionalId <= 0 || a.ProfessionalId == professionalId)
                            && (!excludeAppointmentId.HasValue || a.Id != excludeAppointmentId.Value)
                            && a.StartAt < endAt
                            && a.EndAt > startAt
                            && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.InProgress)
                            && (a.Status != AppointmentStatus.Pending || a.ExpiresAt == null || a.ExpiresAt > now));

            return !hasCollision;
        }

        public async Task ExpireStalePendingAppointmentsAsync(int? companyId = null)
        {
            var now = DateTime.UtcNow;
            var query = _db.Appointments
                .Where(a => a.Status == AppointmentStatus.Pending
                         && a.ExpiresAt != null
                         && a.ExpiresAt <= now);

            if (companyId.HasValue && companyId.Value > 0)
            {
                query = query.Where(a => a.CompanyId == companyId.Value);
            }

            var stale = await query.ToListAsync();
            if (stale.Count > 0)
            {
                foreach (var item in stale)
                {
                    item.Status = AppointmentStatus.Expired;
                    item.ModificationDate = now;
                }
                await _db.SaveChangesAsync();
            }
        }
    }
}
