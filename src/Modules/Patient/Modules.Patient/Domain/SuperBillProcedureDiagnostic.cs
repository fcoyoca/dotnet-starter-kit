namespace FSH.Modules.Patient.Domain;

/// <summary>Links a <see cref="SuperBillProcedure"/> to one incident diagnostic (legacy
/// <c>sbpDiagnosticsID</c>). Bare <see cref="DiagnosticId"/> into the Administration
/// custom-diagnostics catalog — no cross-module FK, mirroring <see cref="PatientIncidentDiagnostic"/>.</summary>
public sealed class SuperBillProcedureDiagnostic
{
    public Guid SuperBillProcedureId { get; private set; }
    public Guid DiagnosticId { get; private set; }

    private SuperBillProcedureDiagnostic() { }

    public static SuperBillProcedureDiagnostic Create(Guid superBillProcedureId, Guid diagnosticId) =>
        new() { SuperBillProcedureId = superBillProcedureId, DiagnosticId = diagnosticId };
}
