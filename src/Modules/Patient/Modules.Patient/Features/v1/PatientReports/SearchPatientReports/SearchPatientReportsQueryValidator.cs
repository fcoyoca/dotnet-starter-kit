using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientReports;

namespace FSH.Modules.Patient.Features.v1.PatientReports.SearchPatientReports;

public sealed class SearchPatientReportsQueryValidator : AbstractValidator<SearchPatientReportsQuery>
{
    public SearchPatientReportsQueryValidator()
    {
        RuleFor(x => x.PageSize).LessThanOrEqualTo(200);
    }
}
