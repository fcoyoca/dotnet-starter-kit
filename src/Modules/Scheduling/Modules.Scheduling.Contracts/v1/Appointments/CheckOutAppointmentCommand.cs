using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Appointments;

public sealed record CheckOutAppointmentCommand(Guid Id) : ICommand<Unit>;
