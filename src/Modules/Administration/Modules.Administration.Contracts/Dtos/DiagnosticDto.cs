namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record DiagnosticDto(
    int Id,
    string Code,
    string? Description,
    string? LongDescription,
    int CodeSourceId,
    string? CodeSourceName,
    bool IsChiropractic,
    bool? IsBillable,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
