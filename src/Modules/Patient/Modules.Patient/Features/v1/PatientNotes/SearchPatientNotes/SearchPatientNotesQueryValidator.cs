using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.SearchPatientNotes;

public sealed class SearchPatientNotesQueryValidator : AbstractValidator<SearchPatientNotesQuery>
{
    public SearchPatientNotesQueryValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
