using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientIncidents;

public sealed record UpdatePatientIncidentCommand(
    Guid IncidentId,
    Guid? IncidentTypeId,
    Guid? DepartmentId,
    DateTime DateOfInitialVisit,
    DateTime DateOfLoss,
    bool IsTransfer,
    bool IsAccident,
    AccidentType? AccidentType,
    string? AccidentState,
    string? Comments,
    string? SummaryOfCare,
    int? AdherenceToPlan,
    IncidentPatientStatus PatientStatus,
    bool IsClosed,
    IReadOnlyList<Guid>? DiagnosticIds) : ICommand;
