using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Appointments;

public sealed record UpdateAppointmentCommand(
    Guid Id,
    Guid ClinicId,
    Guid ProviderId,
    Guid? PatientId,
    Guid? AppointmentTypeId,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Notes,
    bool IsReservation = false,
    string? ReservationTitle = null) : ICommand<Unit>;
