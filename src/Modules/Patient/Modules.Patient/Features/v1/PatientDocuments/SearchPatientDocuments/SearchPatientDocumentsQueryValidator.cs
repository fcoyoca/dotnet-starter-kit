using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientDocuments;

namespace FSH.Modules.Patient.Features.v1.PatientDocuments.SearchPatientDocuments;

public sealed class SearchPatientDocumentsQueryValidator : AbstractValidator<SearchPatientDocumentsQuery>
{
    public SearchPatientDocumentsQueryValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
