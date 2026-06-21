namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record ProcedureCodeDto(
    Guid Id,
    string Code,
    string? Name,
    string? Description,
    Guid ProcedureCategoryId,
    string? ProcedureCategoryName,
    int? CodeSourceId,
    string? CodeSourceName,
    string? MacroText,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
