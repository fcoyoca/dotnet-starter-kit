namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record CustomDiagnosticDto(
    Guid Id,
    string Code,
    string? Description,
    string? LongDescription,
    bool IsChiropractic,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
