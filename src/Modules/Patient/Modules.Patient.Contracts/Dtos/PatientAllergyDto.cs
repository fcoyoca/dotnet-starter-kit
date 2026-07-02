namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record PatientAllergyDto(
    Guid Id,
    Guid PatientId,
    string DrugName,
    string? RxAui,
    string? Reaction,
    string? Comments,
    DateTime DateNoted,
    bool IsActive,
    string? CreatedByName,
    DateTime CreatedAtUtc,
    string? UpdatedByName,
    DateTime? UpdatedAtUtc);
