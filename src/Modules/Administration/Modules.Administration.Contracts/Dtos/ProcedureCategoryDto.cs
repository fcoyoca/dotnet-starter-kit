namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record ProcedureCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsImaging,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
