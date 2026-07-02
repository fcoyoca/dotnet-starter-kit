using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Drugs;

namespace FSH.Modules.Administration.Features.v1.Drugs.UpdateDrug;

public sealed class UpdateDrugCommandValidator : AbstractValidator<UpdateDrugCommand>
{
    public UpdateDrugCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.RxAui).MaximumLength(12);
        RuleFor(x => x.RxCui).MaximumLength(12);
        RuleFor(x => x.Tty).MaximumLength(20);
        RuleFor(x => x.Sab).MaximumLength(40);
        RuleFor(x => x.Code).MaximumLength(64);
    }
}
