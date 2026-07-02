using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.Patients;

public sealed record CreatePatientCommand(
    bool IsActive,
    // Demographics
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
    string? MedicalAlertNotes,
    // Contact
    string? Address1,
    string? Address2,
    string? City,
    string? State,
    string? ZipCode,
    string? Phone,
    string? PhoneExtension,
    string? CellPhone,
    string? Email,
    int? PreferredContactMethodId,
    // PHI (plaintext — encrypted by handler before persistence)
    string? Ssn,
    string? GuardianSsn,
    // Employment (optional)
    string? Occupation,
    string? EmployerName,
    string? EmployerAddress1,
    string? EmployerAddress2,
    string? EmployerCity,
    string? EmployerState,
    string? EmployerZipCode,
    string? EmployerPhone,
    string? EmployerPhoneExtension,
    // Guardian (required when IsMinor = true)
    string? GuardianFirstName,
    string? GuardianLastName,
    string? GuardianMiddleInitial,
    DateTime? GuardianDateOfBirth,
    string? GuardianGender,
    string? GuardianMaritalStatus,
    string? GuardianAddress1,
    string? GuardianAddress2,
    string? GuardianCity,
    string? GuardianState,
    string? GuardianZipCode,
    string? GuardianPhone,
    string? GuardianCellPhone,
    string? GuardianEmployerName,
    string? GuardianEmployerAddress1,
    string? GuardianEmployerAddress2,
    string? GuardianEmployerCity,
    string? GuardianEmployerState,
    string? GuardianEmployerZipCode,
    // Next of Kin (optional)
    string? NextOfKinFirstName,
    string? NextOfKinLastName,
    string? NextOfKinPhone,
    string? NextOfKinRelation,
    string? NextOfKinRelationRoleCode,
    // Insurance (optional)
    string? InsuredFullName,
    DateTime? InsuredDateOfBirth,
    string? InsuredEmployerName,
    int? ReferralTypeId,
    // Flags
    bool HasNoKnownProblems = false,
    bool HasNoKnownMedications = false,
    bool HasNoKnownAllergies = false,
    bool ReceivesEmailReminders = false,
    DateTime? LastVisitDate = null,
    DateTime? NextVisitDate = null,
    // Legacy linkage — source pUniqueID when migrated from BackChart/BronstonChiro; null otherwise.
    long? LegacyUniqueId = null,
    // Patient code. Null/blank on the dashboard's create flow -> the handler
    // auto-generates via IPatientCodeGenerator. The MSSQL migration importer
    // always supplies its own "P-{legacyPId}" value here, which is used as-is.
    string? PatientCode = null) : ICommand<Guid>;
