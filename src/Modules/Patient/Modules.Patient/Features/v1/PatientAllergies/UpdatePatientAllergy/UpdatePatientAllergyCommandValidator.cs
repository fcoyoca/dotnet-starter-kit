using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.UpdatePatientAllergy;

public sealed class UpdatePatientAllergyCommandValidator : AbstractValidator<UpdatePatientAllergyCommand>
{
    public UpdatePatientAllergyCommandValidator()
    {
        RuleFor(x => x.AllergyId).NotEmpty();
        RuleFor(x => x.DrugName).NotEmpty().MaximumLength(512);
        RuleFor(x => x.RxAui).MaximumLength(12);
        RuleFor(x => x.Reaction).MaximumLength(1000);
        RuleFor(x => x.Comments).MaximumLength(4000);
        RuleFor(x => x.DateNoted).NotEmpty();
    }
}
