using System.ComponentModel.DataAnnotations;
using Onion.Common.Models;

namespace Onion.Domain.Appointments
{
    public class AppointmentSettings : BaseEntity, ITenantEntity
    {
        public int CompanyId { get; set; }

        [MaxLength(100)]
        public string TimeZone { get; set; } = "America/Santo_Domingo";

        public AppointmentConfirmationMode ConfirmationMode { get; set; } = AppointmentConfirmationMode.Automatic;

        public int DefaultDurationMinutes { get; set; } = 30;
        public int MinLeadTimeHours { get; set; } = 2;
        public int MaxFutureBookingDays { get; set; } = 30;
        public int HoldSlotTimeoutMinutes { get; set; } = 30;
        public int MinCancelLeadTimeHours { get; set; } = 3;
        public int SlotIntervalMinutes { get; set; } = 10;

        public bool SendReminder24h { get; set; } = true;
        public bool SendReminder1h { get; set; } = true;
        public bool ChannelWhatsApp { get; set; } = true;
        public bool ChannelEmail { get; set; } = true;
    }
}
