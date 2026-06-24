using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Appointments;

public sealed record CreateAppointmentCommand(
    Guid ClinicId,
    Guid ProviderId,
    Guid? PatientId,
    Guid? AppointmentTypeId,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Notes) : ICommand<Guid>;
