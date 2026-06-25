namespace FSH.Modules.Patient.Domain;

public sealed class PatientIncidentDiagnostic
{
    public Guid IncidentId { get; private set; }
    public Guid DiagnosticId { get; private set; }

    private PatientIncidentDiagnostic() { }

    public static PatientIncidentDiagnostic Create(Guid incidentId, Guid diagnosticId) =>
        new() { IncidentId = incidentId, DiagnosticId = diagnosticId };
}
