using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientIncidents;

public sealed record CreatePatientIncidentCommand(
    Guid PatientId,
    Guid? IncidentTypeId,
    Guid? DepartmentId,
    DateTime? DateOfInitialVisit,
    DateTime DateOfLoss,
    bool IsTransfer,
    bool IsAccident,
    AccidentType? AccidentType,
    string? AccidentState,
    string? Comments,
    IReadOnlyList<Guid>? DiagnosticIds) : ICommand<Guid>;
