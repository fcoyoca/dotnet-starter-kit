namespace FSH.Modules.Patient.Contracts.Dtos;

/// <summary>
/// A patient's insurance policy. <see cref="InsuranceCompanyName"/> and <see cref="InsuranceTypeName"/>
/// are resolved from the Administration module for display; <see cref="SubscriberSsnMasked"/> is masked
/// and the SSN is never returned in plaintext.
/// </summary>
public sealed record PatientInsurancePolicyDto(
    Guid Id,
    Guid PatientId,
    Guid InsuranceCompanyId,
    string? InsuranceCompanyName,
    Guid? InsuranceTypeId,
    string? InsuranceTypeName,
    InsurancePriority Priority,
    string? PolicyNumber,
    string? GroupNumber,
    string? MemberId,
    decimal? CoPay,
    decimal? Deductible,
    DateTime? EffectiveDate,
    DateTime? ExpirationDate,
    SubscriberRelationship SubscriberRelationship,
    string? SubscriberFirstName,
    string? SubscriberLastName,
    DateTime? SubscriberDateOfBirth,
    string? SubscriberGender,
    string? SubscriberSsnMasked,
    string? SubscriberEmployerName,
    string? SubscriberAddress1,
    string? SubscriberAddress2,
    string? SubscriberCity,
    string? SubscriberState,
    string? SubscriberZipCode,
    string? Notes,
    bool IsActive,
    string? CreatedByName,
    DateTime CreatedAtUtc,
    string? UpdatedByName,
    DateTime? UpdatedAtUtc);
