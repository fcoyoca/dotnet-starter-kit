using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.RestorePatientIncident;

public sealed class RestorePatientIncidentCommandValidator : AbstractValidator<RestorePatientIncidentCommand>
{
    public RestorePatientIncidentCommandValidator()
    {
        RuleFor(x => x.IncidentId).NotEmpty();
    }
}
