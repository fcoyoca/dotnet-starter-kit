namespace FSH.Modules.Scheduling.Contracts.Dtos;

public sealed record AppointmentDto(
    Guid Id,
    Guid ClinicId,
    Guid ProviderId,
    Guid? PatientId,
    Guid? AppointmentTypeId,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Notes,
    string Status,
    bool Cancelled,
    bool NoShow,
    bool IsReservation,
    string? ReservationTitle);
