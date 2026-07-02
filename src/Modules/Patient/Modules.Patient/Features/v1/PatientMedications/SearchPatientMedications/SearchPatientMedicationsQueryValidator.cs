using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.SearchPatientMedications;

public sealed class SearchPatientMedicationsQueryValidator : AbstractValidator<SearchPatientMedicationsQuery>
{
    public SearchPatientMedicationsQueryValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
