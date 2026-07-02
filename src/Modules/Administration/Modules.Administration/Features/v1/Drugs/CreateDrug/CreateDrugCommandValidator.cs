using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Drugs;

namespace FSH.Modules.Administration.Features.v1.Drugs.CreateDrug;

public sealed class CreateDrugCommandValidator : AbstractValidator<CreateDrugCommand>
{
    public CreateDrugCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.RxAui).MaximumLength(12);
        RuleFor(x => x.RxCui).MaximumLength(12);
        RuleFor(x => x.Tty).MaximumLength(20);
        RuleFor(x => x.Sab).MaximumLength(40);
        RuleFor(x => x.Code).MaximumLength(64);
    }
}
