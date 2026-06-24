using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Appointments;

public sealed record NoShowAppointmentCommand(Guid Id) : ICommand<Unit>;
