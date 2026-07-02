using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.MedicationDoseUnits;

namespace FSH.Modules.Administration.Features.v1.MedicationDoseUnits.UpdateMedicationDoseUnit;

public sealed class UpdateMedicationDoseUnitCommandValidator : AbstractValidator<UpdateMedicationDoseUnitCommand>
{
    public UpdateMedicationDoseUnitCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
    }
}
