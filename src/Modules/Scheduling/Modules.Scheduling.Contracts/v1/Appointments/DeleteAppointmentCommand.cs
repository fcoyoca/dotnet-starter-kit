using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Appointments;

public sealed record DeleteAppointmentCommand(Guid Id) : ICommand<Unit>;
