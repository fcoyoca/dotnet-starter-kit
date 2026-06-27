using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientProblems;

public sealed record CreatePatientProblemCommand(
    Guid PatientId,
    int DiagnosticId,
    string DiagnosticCode,
    string? DiagnosticDescription,
    DateTime? DiagnosisDate,
    ProblemStatus Status,
    string? Notes,
    bool IsMedicalAlert,
    Guid? IncidentId) : ICommand<Guid>;
