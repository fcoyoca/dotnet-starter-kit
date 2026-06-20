namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record ClinicDto(
    Guid Id,
    string Code,
    string Name,
    string Address1,
    string? Address2,
    string City,
    string State,
    string Zip,
    string? Phone,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
