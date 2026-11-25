namespace Onion.Domain.Appointments
{
    public enum AppointmentStatus
    {
        Pending = 0,
        Confirmed = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4,
        RescheduleProposed = 5,
        NoShow = 6,
        Rejected = 7,
        Expired = 8
    }

    public enum AppointmentBillingStatus
    {
        Unbilled = 0,
        Billed = 1
    }

    public enum AppointmentOrigin
    {
        PublicWeb = 0,
        AdminManual = 1
    }

    public enum AppointmentConfirmationMode
    {
        Manual = 0,
        Automatic = 1
    }
}
