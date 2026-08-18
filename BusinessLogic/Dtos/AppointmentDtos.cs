namespace Onion.BussinesLogic.Dtos
{
    public record AppointmentDto(
        int Id,
        System.DateTime StartAt,
        System.DateTime EndAt,
        int? CustomerId,
        int? ServiceId,
        int? ResourceId,
        string? Notes,
        string Status);

    public record CreateAppointmentDto(
        System.DateTime StartAt,
        System.DateTime EndAt,
        int? CustomerId,
        int? ServiceId,
        int? ResourceId,
        string? Notes);

    public record UpdateAppointmentDto(
        int Id,
        System.DateTime StartAt,
        System.DateTime EndAt,
        int? CustomerId,
        int? ServiceId,
        int? ResourceId,
        string? Notes,
        string Status);

    public record ResourceDto(int Id, string Name, string? Type, bool IsActive);

    public record AvailabilityDto(int Id, int ResourceId, System.DateTime StartAt, System.DateTime EndAt, bool IsBlocked);
}
