namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record PatientDetailDto(
    Guid Id,
    string PatientCode,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    PatientDemographicsDto Demographics,
    PatientContactDto Contact,
    PatientPhiDto PHI,
    PatientEmploymentDto? Employment,
    PatientGuardianDto? Guardian,
    PatientNextOfKinDto? NextOfKin,
    PatientInsuranceDto? Insurance,
    bool HasNoKnownProblems,
    bool HasNoKnownMedications,
    bool HasNoKnownAllergies,
    bool ReceivesEmailReminders,
    // Manually-entered planned visit dates (editable on the patient form).
    DateTime? LastVisitDate,
    DateTime? NextVisitDate,
    // Visits derived from the Scheduling module's appointments (read-only, for the chart).
    PatientVisitRefDto? LastVisitAppointment,
    PatientVisitRefDto? NextVisitAppointment);

/// <summary>A visit derived from the schedule: the appointment behind it and its UTC start.</summary>
public sealed record PatientVisitRefDto(Guid AppointmentId, DateTime StartUtc);

public sealed record PatientDemographicsDto(
    string FirstName,
    string LastName,
    string? MiddleInitial,
    DateTime DateOfBirth,
    string Gender,
    string? MaritalStatus,
    bool IsMinor,
    int? RaceId,
    int? EthnicityId,
    int? LanguageId,
    int? SmokingStatusId,
    DateTime? SmokingStartDate,
    DateTime? SmokingEndDate,
    string? MedicalAlertNotes);

public sealed record PatientContactDto(
    string? Address1,
    string? Address2,
    string? City,
    string? State,
    string? ZipCode,
    string? Phone,
    string? PhoneExtension,
    string? CellPhone,
    string? Email,
    int? PreferredContactMethodId);

/// <summary>SSN is masked for display; never returned in plaintext.</summary>
public sealed record PatientPhiDto(
    string? SsnMasked);

public sealed record PatientEmploymentDto(
    string? Occupation,
    string? EmployerName,
    string? EmployerAddress1,
    string? EmployerAddress2,
    string? EmployerCity,
    string? EmployerState,
    string? EmployerZipCode,
    string? EmployerPhone,
    string? EmployerPhoneExtension);

public sealed record PatientGuardianDto(
    string? FirstName,
    string? LastName,
    string? MiddleInitial,
    DateTime? DateOfBirth,
    string? Gender,
    string? MaritalStatus,
    string? Address1,
    string? Address2,
    string? City,
    string? State,
    string? ZipCode,
    string? Phone,
    string? CellPhone,
    string? EmployerName,
    string? EmployerAddress1,
    string? EmployerAddress2,
    string? EmployerCity,
    string? EmployerState,
    string? EmployerZipCode);

public sealed record PatientNextOfKinDto(
    string? FirstName,
    string? LastName,
    string? Phone,
    string? Relation,
    string? RelationRoleCode);

public sealed record PatientInsuranceDto(
    string? InsuredFullName,
    DateTime? InsuredDateOfBirth,
    string? InsuredEmployerName,
    int? ReferralTypeId);
