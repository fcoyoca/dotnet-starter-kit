using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.AppointmentTypes;

public sealed record CreateAppointmentTypeCommand(
    string Name,
    int DefaultDurationMinutes,
    string? Color = null,
    int DisplayOrder = 0) : ICommand<Guid>;
