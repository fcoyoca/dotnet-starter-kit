using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.CreatePatientAllergy;

public sealed class CreatePatientAllergyCommandValidator : AbstractValidator<CreatePatientAllergyCommand>
{
    public CreatePatientAllergyCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DrugName).NotEmpty().MaximumLength(512);
        RuleFor(x => x.RxAui).MaximumLength(12);
        RuleFor(x => x.Reaction).MaximumLength(1000);
        RuleFor(x => x.Comments).MaximumLength(4000);
        RuleFor(x => x.DateNoted).NotEmpty();
    }
}
