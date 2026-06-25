using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.DeletePatientIncident;

public sealed class DeletePatientIncidentCommandValidator : AbstractValidator<DeletePatientIncidentCommand>
{
    public DeletePatientIncidentCommandValidator()
    {
        RuleFor(x => x.IncidentId).NotEmpty();
    }
}
