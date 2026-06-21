namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record DiagnosticCategoryDto(
    Guid Id,
    string Name,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
