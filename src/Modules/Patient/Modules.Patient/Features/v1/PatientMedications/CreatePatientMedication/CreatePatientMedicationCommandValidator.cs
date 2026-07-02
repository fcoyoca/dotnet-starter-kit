using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.CreatePatientMedication;

public sealed class CreatePatientMedicationCommandValidator : AbstractValidator<CreatePatientMedicationCommand>
{
    public CreatePatientMedicationCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DrugName).NotEmpty().MaximumLength(512);
        RuleFor(x => x.RxAui).MaximumLength(12);
        RuleFor(x => x.RxCode).MaximumLength(12);
        RuleFor(x => x.Ndc).MaximumLength(24);
        RuleFor(x => x.Prescriber).MaximumLength(256);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
        RuleFor(x => x.DoseValue).GreaterThan(0).When(x => x.DoseValue.HasValue);
        RuleFor(x => x.DosePeriodValue).GreaterThan(0).When(x => x.DosePeriodValue.HasValue);
        RuleFor(x => x.DosePeriodUnit).MaximumLength(16);
        RuleFor(x => x.Instructions).MaximumLength(4000);
        RuleFor(x => x.Indication).MaximumLength(4000);
    }
}
