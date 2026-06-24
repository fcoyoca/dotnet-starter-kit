using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Appointments;

public sealed record CheckInAppointmentCommand(Guid Id) : ICommand<Unit>;
