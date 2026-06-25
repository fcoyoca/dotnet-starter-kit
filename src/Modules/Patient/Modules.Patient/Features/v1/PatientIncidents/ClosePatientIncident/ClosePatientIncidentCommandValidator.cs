using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.ClosePatientIncident;

public sealed class ClosePatientIncidentCommandValidator : AbstractValidator<ClosePatientIncidentCommand>
{
    public ClosePatientIncidentCommandValidator()
    {
        RuleFor(x => x.IncidentId).NotEmpty();
    }
}
