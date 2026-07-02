using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.SearchPatientAllergies;

public sealed class SearchPatientAllergiesQueryValidator : AbstractValidator<SearchPatientAllergiesQuery>
{
    public SearchPatientAllergiesQueryValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
