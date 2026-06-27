using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientProblems;

namespace FSH.Modules.Patient.Features.v1.PatientProblems.SearchPatientProblems;

public sealed class SearchPatientProblemsQueryValidator : AbstractValidator<SearchPatientProblemsQuery>
{
    public SearchPatientProblemsQueryValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
