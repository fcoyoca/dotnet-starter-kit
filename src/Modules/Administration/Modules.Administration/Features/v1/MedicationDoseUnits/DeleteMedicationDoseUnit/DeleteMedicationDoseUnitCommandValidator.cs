using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.MedicationDoseUnits;

namespace FSH.Modules.Administration.Features.v1.MedicationDoseUnits.DeleteMedicationDoseUnit;

public sealed class DeleteMedicationDoseUnitCommandValidator : AbstractValidator<DeleteMedicationDoseUnitCommand>
{
    public DeleteMedicationDoseUnitCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
