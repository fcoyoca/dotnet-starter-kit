namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record InsuranceTypeDto(
    Guid Id,
    string Name,
    bool IsActive,
    Guid? ProcedureCategoryId,
    string? ProcedureCategoryName,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
