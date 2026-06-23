namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record AppointmentTypeDto(
    Guid Id,
    string Name,
    string? Color,
    int DefaultDurationMinutes,
    int DisplayOrder,
    bool IsActive);
