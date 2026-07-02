using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Drugs;

namespace FSH.Modules.Administration.Features.v1.Drugs.DeleteDrug;

public sealed class DeleteDrugCommandValidator : AbstractValidator<DeleteDrugCommand>
{
    public DeleteDrugCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
