using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientIncidents;

/// <summary>Replace the set of diagnostic codes (CustomDiagnostic ids) associated with an incident.</summary>
public sealed record SetIncidentDiagnosticsCommand(Guid IncidentId, IReadOnlyList<Guid> DiagnosticIds) : ICommand<Unit>;
