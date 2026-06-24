using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Appointments;

public sealed record CancelAppointmentCommand(Guid Id) : ICommand<Unit>;
