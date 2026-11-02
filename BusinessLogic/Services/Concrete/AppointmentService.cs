using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain.Appointments;
using Onion.DataAccess;
using Onion.Common.Exceptions;
using Onion.Common.Models;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IRepository<Appointment> _repo;
        private readonly OnionDbContext _db;
        private readonly ITenantProvider _tenantProvider;

        public AppointmentService(IRepository<Appointment> repo, OnionDbContext db, ITenantProvider tenantProvider)
        {
            _repo = repo;
            _db = db;
            _tenantProvider = tenantProvider;
        }

        public async Task<AppointmentDto?> GetByIdAsync(int id)
        {
            var e = await _repo.GetByIdAsync(id);
            if (e == null) return null;
            return MapToDto(e);
        }

        public async Task<IEnumerable<AppointmentDto>> ListAsync()
        {
            var list = await _repo.ListAsync();
            return list.Select(MapToDto);
        }

        public async Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto)
        {
            var companyId = _tenantProvider.GetCompanyId();
            if (companyId is not > 0)
                throw new TenantRequiredException("A company context is required to create an appointment.");
            if (dto.EndAt <= dto.StartAt)
                throw new CustomException(new Error { Code = "INVALID_APPOINTMENT_RANGE", Message = "End time must be after start time.", Language = "EN" });

            // Validate overlapping
            if (dto.ResourceId.HasValue)
            {
                var overlap = _db.Appointments.Any(a => a.ResourceId == dto.ResourceId && a.StartAt < dto.EndAt && a.EndAt > dto.StartAt);
                if (overlap)
                    throw new InvalidOperationException("Overlap with existing appointment");
            }

            var e = new Appointment
            {
                StartAt = dto.StartAt,
                EndAt = dto.EndAt,
                CompanyId = companyId.Value,
                CustomerId = dto.CustomerId,
                ServiceId = dto.ServiceId,
                ResourceId = dto.ResourceId,
                Notes = dto.Notes
            };

            await _repo.AddAsync(e);
            await _db.SaveChangesAsync(System.Threading.CancellationToken.None);
            return MapToDto(e);
        }

        public async Task<AppointmentDto> UpdateAsync(UpdateAppointmentDto dto)
        {
            if (dto.EndAt <= dto.StartAt)
                throw new CustomException(new Error { Code = "INVALID_APPOINTMENT_RANGE", Message = "End time must be after start time.", Language = "EN" });
            var existing = await _repo.GetByIdAsync(dto.Id);
            if (existing == null) throw new KeyNotFoundException("Appointment not found");

            if (dto.ResourceId.HasValue)
            {
                var overlap = _db.Appointments.Any(a => a.ResourceId == dto.ResourceId && a.Id != dto.Id && a.StartAt < dto.EndAt && a.EndAt > dto.StartAt);
                if (overlap) throw new InvalidOperationException("Overlap with existing appointment");
            }

            existing.StartAt = dto.StartAt;
            existing.EndAt = dto.EndAt;
            existing.CustomerId = dto.CustomerId ?? existing.CustomerId;
            existing.ServiceId = dto.ServiceId ?? existing.ServiceId;
            existing.ResourceId = dto.ResourceId ?? existing.ResourceId;
            existing.Notes = dto.Notes;
            if (Enum.TryParse<AppointmentStatus>(dto.Status, true, out var st)) existing.Status = st;

            _repo.Update(existing);
            await _db.SaveChangesAsync(System.Threading.CancellationToken.None);
            return MapToDto(existing);
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return;
            _repo.Remove(existing);
            await _db.SaveChangesAsync(System.Threading.CancellationToken.None);
        }

        private static AppointmentDto MapToDto(Appointment e)
        {
            return new AppointmentDto(e.Id, e.StartAt, e.EndAt, e.CustomerId, e.ServiceId, e.ResourceId, e.Notes, e.Status.ToString());
        }
    }
}
