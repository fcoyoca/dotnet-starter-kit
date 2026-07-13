using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;

public sealed record CreatePatientInsurancePolicyCommand(
    Guid PatientId,
    Guid InsuranceCompanyId,
    Guid? InsuranceTypeId,
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
    string? SubscriberSsn,
    string? SubscriberEmployerName,
    string? SubscriberAddress1,
    string? SubscriberAddress2,
    string? SubscriberCity,
    string? SubscriberState,
    string? SubscriberZipCode,
    string? Notes,
    bool IsActive = true) : ICommand<Guid>;
