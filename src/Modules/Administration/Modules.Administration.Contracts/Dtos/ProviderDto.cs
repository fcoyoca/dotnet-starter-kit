namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record ProviderDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Prefix,
    string? Suffix,
    string? Specialty,
    string? Npi,
    string? KareoExternalId,
    Guid? PrimaryClinicId,
    string? PrimaryClinicName,
    string? UserId,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    string? SignatureImagePath = null,
    string? SignatureImageUrl = null);
