using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.SetIncidentDiagnostics;

public sealed class SetIncidentDiagnosticsCommandValidator : AbstractValidator<SetIncidentDiagnosticsCommand>
{
    public SetIncidentDiagnosticsCommandValidator()
    {
        RuleFor(x => x.IncidentId).NotEmpty();
        RuleFor(x => x.DiagnosticIds).NotNull();
        RuleForEach(x => x.DiagnosticIds).NotEmpty();
    }
}
