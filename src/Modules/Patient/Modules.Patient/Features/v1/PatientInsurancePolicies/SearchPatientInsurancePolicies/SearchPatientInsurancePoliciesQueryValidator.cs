using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;

namespace FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.SearchPatientInsurancePolicies;

public sealed class SearchPatientInsurancePoliciesQueryValidator
    : AbstractValidator<SearchPatientInsurancePoliciesQuery>
{
    public SearchPatientInsurancePoliciesQueryValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
