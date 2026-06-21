namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record InsuranceTypeProcedureDto(
    Guid Id,
    Guid InsuranceTypeId,
    Guid ProcedureCodeId,
    string ProcedureCode,
    string? ProcedureName,
    string? ProcedureCategoryName,
    decimal Price,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
