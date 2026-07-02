using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.MedicationDoseUnits;

namespace FSH.Modules.Administration.Features.v1.MedicationDoseUnits.CreateMedicationDoseUnit;

public sealed class CreateMedicationDoseUnitCommandValidator : AbstractValidator<CreateMedicationDoseUnitCommand>
{
    public CreateMedicationDoseUnitCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
    }
}
