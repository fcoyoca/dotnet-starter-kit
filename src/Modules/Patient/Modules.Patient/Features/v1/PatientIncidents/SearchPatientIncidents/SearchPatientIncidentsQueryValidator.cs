using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.SearchPatientIncidents;

public sealed class SearchPatientIncidentsQueryValidator : AbstractValidator<SearchPatientIncidentsQuery>
{
    public SearchPatientIncidentsQueryValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.PageSize).LessThanOrEqualTo(200);
    }
}
