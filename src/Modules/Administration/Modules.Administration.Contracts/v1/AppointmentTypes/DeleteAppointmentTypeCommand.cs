using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.AppointmentTypes;

public sealed record DeleteAppointmentTypeCommand(Guid Id) : ICommand<Unit>;
