namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record IncidentTypeDto(
    Guid Id,
    string Name,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
