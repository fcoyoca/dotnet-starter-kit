namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record InsuranceCompanyDto(
    Guid Id,
    string Name,
    Guid? InsuranceTypeId,
    string? InsuranceTypeName,
    int FormularyTiers,
    string? Address1,
    string? Address2,
    string? City,
    string? State,
    string? Zip,
    string? Phone,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
