using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Onion.Common.Models;
using Onion.Domain;

namespace Onion.Domain.Appointments
{
    public class Appointment : BaseEntity, ITenantEntity
    {
        public int CompanyId { get; set; }

        [MaxLength(50)]
        public string? BookingCode { get; set; }

        [MaxLength(64)]
        public string? ManagementTokenHash { get; set; }

        public DateTime? ManagementTokenExpiresAt { get; set; }

        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
        public AppointmentBillingStatus BillingStatus { get; set; } = AppointmentBillingStatus.Unbilled;
        public AppointmentOrigin Origin { get; set; } = AppointmentOrigin.PublicWeb;

        // Customer
        public int? CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        [MaxLength(200)]
        public string? CustomerName { get; set; }

        [MaxLength(50)]
        public string? CustomerPhone { get; set; }

        [MaxLength(200)]
        public string? CustomerEmail { get; set; }

        // Service & Professional
        public int? AppointmentServiceId { get; set; }
        public virtual AppointmentService? AppointmentService { get; set; }

        public int? ProfessionalId { get; set; }
        public virtual AppointmentProfessional? Professional { get; set; }

        // Legacy fields preserved for backward compatibility
        public int? ServiceId { get; set; }
        public int? ResourceId { get; set; }

        // Invoicing relationship (1-to-1 unique)
        public int? SaleId { get; set; }
        public virtual Sale? Sale { get; set; }

        public DateTime? ExpiresAt { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        // Navigations
        public virtual ICollection<AppointmentStatusHistory>? StatusHistories { get; set; }
        public virtual ICollection<AppointmentRescheduleRequest>? RescheduleRequests { get; set; }
        public virtual ICollection<AppointmentNotification>? Notifications { get; set; }
    }
}
