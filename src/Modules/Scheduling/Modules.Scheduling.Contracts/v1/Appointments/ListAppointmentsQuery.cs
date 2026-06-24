using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Appointments;

public sealed record ListAppointmentsQuery(
    Guid ClinicId,
    IReadOnlyList<Guid>? ProviderIds,
    DateTime FromUtc,
    DateTime ToUtc) : IQuery<IReadOnlyList<AppointmentDto>>;
