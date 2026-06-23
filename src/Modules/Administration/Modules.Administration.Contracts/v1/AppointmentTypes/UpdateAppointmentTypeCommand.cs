using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.AppointmentTypes;

public sealed record UpdateAppointmentTypeCommand(
    Guid Id,
    string Name,
    int DefaultDurationMinutes,
    string? Color,
    int DisplayOrder,
    bool IsActive) : ICommand<Unit>;
