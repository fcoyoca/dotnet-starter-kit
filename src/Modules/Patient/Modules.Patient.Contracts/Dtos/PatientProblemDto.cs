namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record PatientProblemDto(
    Guid Id,
    Guid PatientId,
    Guid? IncidentId,
    int DiagnosticId,
    string DiagnosticCode,
    string? DiagnosticDescription,
    DateTime? DiagnosisDate,
    ProblemStatus Status,
    string? Notes,
    bool IsMedicalAlert,
    string? CreatedByName,
    DateTime CreatedAtUtc,
    string? UpdatedByName,
    DateTime? UpdatedAtUtc);
