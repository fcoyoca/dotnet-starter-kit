using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Appointments;

public sealed record ConfirmAppointmentCommand(Guid Id) : ICommand<Unit>;
